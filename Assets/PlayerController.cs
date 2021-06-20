using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    //Sprite stuff
    private SpriteRenderer sprite;    
    private AnimationController animationController;


    //Physics constants
    [SerializeField]
    private float movementSpeed = 64f; //in pixels per second
    [SerializeField]
    private float groundCheckRadius;
    [SerializeField]
    private float jumpSpeed = 256f; //in pixels per second
    [SerializeField]
    private float airAcceleration = 32f;
    [SerializeField]
    private float airFriction = 32f;
    [SerializeField]
    private float maxAirSpeed = 256f;
    [SerializeField]
    private float wallSlideSpeed = 32f;//in pixels per second
    [SerializeField]
    private float wallJumpSpeed = 16f;
    [SerializeField]
    private float wallSlideFriction = 0.25f;
    [SerializeField]
    private float wallCheckDistance = 13f;
    public float wallSlideFallThreshold;
    [SerializeField]
    private float slopeCheckDistance;
    [SerializeField]
    private float maxSlopeAngle;
    [SerializeField]
    private float gravity = 700f;

    [SerializeField]
    private Transform groundCheck;
    [SerializeField]
    private LayerMask groundMask;
    [SerializeField]
    private LayerMask playerMask;
    [SerializeField]
    private PhysicsMaterial2D noFriction;
    [SerializeField]
    private PhysicsMaterial2D fullFriction;
    
    private Vector2 input; 
    private float slopeDownAngle;
    private float slopeSideAngle;
    private float lastSlopeAngle;
    private float facing = 1f;

    //Physics Bools
    [SerializeField]
    private bool isGrounded = false;
    [SerializeField]
    private bool isOnSlope = false;
    private bool isJumping = false;
    public bool isWallSlide = false;
    private bool canWalkOnSlope = false;
    [SerializeField]
    private bool canJump = false;
    [SerializeField]
    private bool canWallJump = false;

    //Physics Vectors
    private Vector2 vel;
    private Vector2 colliderSize;
    private Vector2 slopeNormalPerp;

    //Physics references
    private new Rigidbody2D rigidbody;
    [SerializeField]
    private new CapsuleCollider2D collider;

    public float tScale = 1f;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        rigidbody = GetComponent<Rigidbody2D>();    
        collider = GetComponent<CapsuleCollider2D>();
        input = Vector2.zero;
        animationController = GetComponent<AnimationController>();
        
    }

    // Update is called once per frame
    void Update()
    {
        Time.timeScale = tScale;
        groundCheckRadius = (collider.size.x / 2f) - 2f;
        groundCheck.localPosition = new Vector3(groundCheck.localPosition.x, (groundCheckRadius) -3f, 0);
        CheckInput();
    }
    
    void FixedUpdate()
    {
        CheckGround();
        CheckWall();
        SlopeCheck();
        ApplyMovement();
    }

    void CheckInput()
    {
        input = new Vector2((Input.GetAxisRaw("Horizontal")), Input.GetAxisRaw("Vertical"));
        if(isGrounded)
        {
            ForceSpriteFlip();
            if(input.x == 0)
            {
                animationController.Idle();
            }
            else
            {
                animationController.Run();
            }
        }
        else
        {
            if(isWallSlide)
            {
                if(rigidbody.velocity.y > 0)
                {
                  animationController.WallSlideUp();
                }
                else if(rigidbody.velocity.y < -wallSlideFallThreshold)
                {
                    animationController.WallSlideDown();
                }
                else
                {
                    animationController.WallSlideMid();
                }
            }
            else
            {
                MoveSpriteFlip();
                if(rigidbody.velocity.y >= 0)
                {
                    if(input.x != 0)
                    {
                        animationController.MoveAir();
                    }
                }
                else
                {
                    animationController.Falling();  
                }
            }    
        }

        if(Input.GetButtonDown("Jump"))
        {
            Jump();
        }
    }

    void CheckWall()
    {
        isWallSlide = false;
        if(!isGrounded)
        {
            LayerMask nonPlayer = ~playerMask;
            RaycastHit2D hit = Physics2D.Raycast(PositionOffset(), WallCheckVector(), wallCheckDistance, LayerMask.GetMask("Ground"));
            if(hit)
            {
                isWallSlide = true;
                canWallJump = true;
            }
            else
            {
                canWallJump = false;
            }
        }
    }

    void CheckGround()
    {
        bool lastFrameAir = !isGrounded;
        RaycastHit2D groundCheckCircle = Physics2D.CircleCast(groundCheck.position, groundCheckRadius, Vector2.zero, 0f, (int)groundMask);

        //if the overlap with the ground is very slight, push the player away from the contact point to force into a fall state
        //this is kind of a quick hack
        if(groundCheckCircle)
        {               
            //check the ground in a slightly more narrow (groundCheckPixelOffset difference) way, to see if the character needs to be budged away from a ledge
            //TODO?: instead of budge away, change to auto ledge grab?

            float groundCheckPixelOffset = 2f; 
            bool leftGround = Physics2D.Raycast(groundCheck.position - new Vector3(groundCheckRadius - groundCheckPixelOffset,0,0), Vector2.down, groundCheckRadius, groundMask);
            bool rightGround = Physics2D.Raycast(groundCheck.position + new Vector3(groundCheckRadius - groundCheckPixelOffset,0,0), Vector2.down, groundCheckRadius, groundMask);

            //if neither leftGround nor rightGround detect ground, that means we're just barely on a ledge and should just fall
            if(!leftGround && !rightGround)
            {
                isGrounded = false;

                //find x-difference between position and collision point
                float collisionOffsetX = transform.position.x - groundCheckCircle.point.x;
                transform.position += new Vector3(Mathf.Sign(collisionOffsetX)*groundCheckPixelOffset, 0f, 0f);
            }
            //else, we're probably grounded adequately and can continue as normal
            else
            {
                isGrounded = true;
            }
        }
        else
        {
            isGrounded = false;
        }
        

        if(isGrounded)
        {
            if(lastFrameAir)
                animationController.Landing();
        }

        vel = rigidbody.velocity;
        if(rigidbody.velocity.y <= 0f)
        {
            isJumping = false;
        }
        if(isGrounded && !isJumping && slopeDownAngle <= maxSlopeAngle)
        {        
            canJump = true;
        }
        else
        {
            canJump = false;
        }
    }

    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position;
        //checkPos.y -= colliderSize.y / 2f;

        SlopeCheckHorizontal(checkPos);
        SlopeCheckVertical(checkPos);
    }

    private void SlopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, groundMask);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, groundMask);

        if(slopeHitFront)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        }
        else if (slopeHitBack)
        {
            isOnSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        }
        else
        {
            isOnSlope = false;
            slopeSideAngle = 0f;
        }

        Debug.DrawRay(slopeHitFront.point, slopeHitFront.normal, Color.cyan);
        Debug.DrawRay(slopeHitBack.point, slopeHitBack.normal, Color.yellow);
    }

    private void SlopeCheckVertical(Vector2 checkPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, groundMask);

        if(hit)
        {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if(slopeDownAngle != lastSlopeAngle)
            {
                isOnSlope = true;
            }

            lastSlopeAngle = slopeDownAngle;

            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }

        if(slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle)
        {
            canWalkOnSlope = false;
        }
        else
        {
            canWalkOnSlope = true;
        }

        //slope slide physics
        if(isOnSlope && canWalkOnSlope && input.x == 0.0f)
        {
            rigidbody.sharedMaterial = fullFriction;
        }
        else
        {
            rigidbody.sharedMaterial = noFriction;
        }
    }
    
    void ApplyMovement()
    {
        //on ground
        if(isGrounded)
        {
            //ground is flat
            if (!isOnSlope && !isJumping)
            {
                rigidbody.velocity = new Vector2(movementSpeed * input.x, 0.0f);
            }
            //ground is sloped
            else if (isOnSlope)
            {
                //slope too steep to walk on
                if(!canWalkOnSlope)
                {
                    rigidbody.velocity = new Vector2(rigidbody.velocity.x, rigidbody.velocity.y - (gravity * Time.deltaTime));
                }
                else
                {
                    //not jumping on slope
                    if(!isJumping)
                    {
                        rigidbody.velocity = new Vector2(movementSpeed * slopeNormalPerp.x * -input.x, 
                                                        movementSpeed * slopeNormalPerp.y * -input.x);
                    }
                }
            } 
        }
        //in the air
        else
        {
            //if against a wall
            if(isWallSlide)
            {
                rigidbody.velocity = new Vector2(0, (rigidbody.velocity.y - gravity * Time.deltaTime) );

                if(rigidbody.velocity.y < -wallSlideSpeed)
                {
                    rigidbody.velocity = new Vector2(0, -wallSlideSpeed);
                }
                // else if(rigidbody.velocity.y > wallSlideSpeed)
                // {
                //     rigidbody.velocity = new Vector2(0, wallSlideSpeed);
                // }
                else
                {
                    rigidbody.velocity = new Vector2(0, rigidbody.velocity.y);
                }
            }
            //handle air movement
            else
            {
                //change in x-velocity gradual
                rigidbody.velocity = new Vector2((rigidbody.velocity.x + (input.x * Time.deltaTime * airAcceleration)), rigidbody.velocity.y - (gravity * Time.deltaTime));

                //apply air friction
                if(rigidbody.velocity.sqrMagnitude >= airFriction)
                {
                    //rigidbody.velocity -= new Vector2(facing * airFriction * Time.deltaTime, 0f);
                }

                //hard cap on air speed
                if(Mathf.Abs(rigidbody.velocity.x) > maxAirSpeed)
                {
                    rigidbody.velocity = new Vector2(facing * maxAirSpeed, rigidbody.velocity.y);
                }
            }
        }
    }
    
    //JumpMethod
    void Jump()
    {
        if(canJump)
        {
            canJump = false;
            isJumping = true;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, jumpSpeed);
            
            animationController.Jump();
        }
        else if (canWallJump)
        {
            canWallJump = false;
            isWallSlide = false;
            isJumping = true;

            //flip sprite and push away to unlock from wall
            facing *= -1;
            transform.localScale = new Vector3(facing, transform.localScale.y, transform.localScale.z);
            transform.position += new Vector3(facing * 8, 0, 0);

            //since sprite is flipped, jump in the direction currently facing
            rigidbody.velocity = new Vector2(wallJumpSpeed * facing, jumpSpeed);
            
            animationController.Jump();
        }
    }
    void ForceSpriteFlip()
    {
        if((facing == -1 && input.x > 0f) || (facing == 1 && input.x < 0f))
        {
            facing *= -1;
            transform.localScale = new Vector3(facing, transform.localScale.y, transform.localScale.z);
            animationController.Turn();
        }
    }
    void MoveSpriteFlip()
    {
        if((facing == -1 && rigidbody.velocity.x > 0f) || (facing == 1 && rigidbody.velocity.x < 0f))
        {
            facing *= -1;
            transform.localScale = new Vector3(facing, transform.localScale.y, transform.localScale.z);
            animationController.Turn();
        }
    }

    Vector2 PositionOffset()
    {
        Vector2 offsetPos = (Vector2)transform.position;
        offsetPos.x += facing * collider.offset.x;
        offsetPos.y += 1f; //prevent colliding with ground
        return offsetPos;
    }

    Vector2 WallCheckVector()
    {
        return new Vector2(facing * wallCheckDistance, 0f);
    }

    private void OnDrawGizmos()
    {
        if(groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        //wallSlide check
        Gizmos.DrawRay(PositionOffset(), WallCheckVector());
    }
}
