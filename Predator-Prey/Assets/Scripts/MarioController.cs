using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1. 8-directional movement
/// 2. stop and face current direction when input is absent
/// </summary>
/// 
public class MarioController : MonoBehaviour
{   public float velocity = 5;
    public float turnSpeed = 10;
    public float heightPadding = 0.05f;
    public LayerMask ground;
    public float maxGroundAngle = 120;
    public bool debug;

    Vector2 input;
    float angle;
    float groundAngle;

    Quaternion targetRotation;
    Transform cam;

    Vector3 forward;
    RaycastHit hitInfo;
    bool grounded;

    void Start() {
        cam = Camera.main.transform;
    }

    void Update() {
        GetInput();
        CalculateDirection();
        CalculateForward();
        CalculateGroundAngle();
        ChechGround();
        ApplyGravity();
        Drawdebuglines();

        if( Mathf.Abs(input.x) < 1 && Mathf.Abs(input.y) < 1 ) return;

        
        Rotate();
        Move();
    }

    /// <summary>
    /// Input base on Horizontal(a,d,<,>) and Vertical(w,s,^,v) keys
    /// </summary>
    void GetInput() {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
    }

    /// <summary>
    /// Direction relateive to the camera's rotation
    /// </summary>
    void CalculateDirection(){
        angle = Mathf.Atan2(input.x, input.y);
        angle = Mathf.Rad2Deg * angle;

        angle += cam.eulerAngles.y;
    }

    /// <summary>
    /// Rotate toward the calculated angle
    /// </summary>
    void Rotate(){
        targetRotation = Quaternion.Euler(0, angle, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// This player only move along its own forward axis
    /// </summary>
    void Move(){
        transform.position += transform.forward * velocity * Time.deltaTime;
    }

    /// <summary>
    /// If player is not grounded, forward will be equal to transform forward
    /// Use a cross product to determine the new forward vector
    /// </summary>
    void CalculateForward() {
        if(!grounded) {
            forward = transform.forward;
            return;
        }

        forward = Vector3.Cross(hitInfo.normal, -transform.right);
    }

    /// <summary>
    /// Use a vector3 angle between the ground normal and the tranform forward to determine the slope of the ground
    /// </summary>
    void CalculateGroundAngle() {
        if(!grounded) {
            groundAngle = 90;
            return;
        }

        groundAngle = Vector3.Angle(hitInfo.normal, transform.forward);
    }

    /// <summary>
    /// Use a raycast of length height to determine whether or not the player is grounded
    /// </summary>
    void ChechGround() {
        if(Physics.Raycast(transform.position, -Vector3.up, out hitInfo, heightPadding, ground)) {

            grounded = true;
        }

        else {
            grounded = false;
        }

    }

    /// <summary>
    /// If nor grounded, the player should fall
    /// </summary>
    void ApplyGravity() {

    }

    void Drawdebuglines(){

    }
}
