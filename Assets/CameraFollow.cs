using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    private Rigidbody2D rb2D;
    private Vector3 targetPos = Vector3.zero;
    public Vector2 targetOffset = Vector2.zero;
    private Vector2 lerpVector = Vector2.zero;
    public float camSpeed = 5f;
    public Vector2 deadZone;

    // Start is called before the first frame update
    void Start()
    {
        if(target != null)
        {
            rb2D = target.GetComponent<Rigidbody2D>();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(target != null)
        {   
            targetPos = new Vector3(target.position.x + (target.localScale.x * targetOffset.x), target.position.y + targetOffset.y, transform.position.z);
            if(Mathf.Abs(transform.position.x - targetPos.x) > deadZone.x || Mathf.Abs(transform.position.y - targetPos.y) > deadZone.y)
            {
                lerpVector = new Vector2(Mathf.Lerp(transform.position.x, targetPos.x, camSpeed * Time.deltaTime),
                                         Mathf.Lerp(transform.position.y, targetPos.y, ((Mathf.Abs(transform.position.y - targetPos.y) / 48f) + 1) * camSpeed * Time.deltaTime));
                transform.position = new Vector3(lerpVector.x, lerpVector.y, transform.position.z);
            }
        }
    }
}
