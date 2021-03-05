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
    private float slopeCheckDistance;
    [SerializeField]
    private float maxSlopeAngle;
    [SerializeField]
    private Transform groundCheck;
    [SerializeField]
    private LayerMask whatIsGround;
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
    private bool isGrounded = false;
    private bool isOnSlope = false;
    private bool isJumping = false;
    private bool canWalkOnSlope = false;
    [SerializeField]
    private bool canJump = false;
    private Vector2 vel;
    private Vector2 colliderSize;
    private Vector2 slopeNormalPerp;

    //Physics references
    private new Rigidbody2D rigidbody;
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
        SlopeCheck();
        ApplyMovement();
    }

    void CheckInput()
    {
        input = new Vector2((Input.GetAxisRaw("Horizontal")), Input.GetAxisRaw("Vertical"));
        SpriteFlip();
        if(isGrounded)
        {
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

        if(Input.GetButtonDown("Jump"))
        {
            Jump();
        }
    }

    void CheckGround()
    {
        bool lastFrameAir = !isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        if(lastFrameAir && isGrounded)
        {
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
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, whatIsGround);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, whatIsGround);

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
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, whatIsGround);

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
        //on flat ground
        if (isGrounded && !isOnSlope && !isJumping)
        {
            rigidbody.velocity = new Vector2(movementSpeed * input.x, 0.0f);
        }
        //on sloped ground
        else if (isGrounded && isOnSlope && canWalkOnSlope && !isJumping)
        {
            rigidbody.velocity = new Vector2(movementSpeed * slopeNormalPerp.x * -input.x, 
                                             movementSpeed * slopeNormalPerp.y * -input.x);
        }
        //in the air
        else if(!isGrounded)
        {
            rigidbody.velocity = new Vector2(movementSpeed * input.x, rigidbody.velocity.y);
        }
    }
    
    //JumpMethod
    void Jump()
    {
        if(canJump)
        {
            canJump = false;
            isJumping = true;
            rigidbody.velocity = new Vector2(0f, jumpSpeed);
            animationController.Jump();
        }
    }
    void SpriteFlip()
    {
        if((facing == -1 && input.x > 0f) || (facing == 1 && input.x < 0f))
        {
            facing *= -1;
            transform.localScale = new Vector3(facing, transform.localScale.y, transform.localScale.z);
            animationController.Turn();
        }
    }

    private void OnDrawGizmos()
    {
        if(groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
