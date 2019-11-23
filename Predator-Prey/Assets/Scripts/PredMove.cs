using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredMove : MonoBehaviour
{
    // Enumerate the Predator movement states
    // *** need to determine how to handle jumps ***
    public enum PredatorMove
    {
        Rest,
        Idle,
        Wander,
        Hunt,
        Pursue
    };

    bool isJumping = false;
    public Predator pred;
    public Rigidbody rb;

    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        
        //rb.velocity = transform.forward * pred.moveSpeed;
        //rb.AddForce(transform.forward * pred.moveSpeed, ForceMode.VelocityChange);

        
        if(!isJumping)
        {
           isJumping = true;
           pred.Jump(new Vector3(0.0f, Mathf.Sin(45.0f * Mathf.Deg2Rad), Mathf.Cos(45.0f * Mathf.Deg2Rad)));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        pred = GetComponent<Predator>();
        rb = GetComponent<Rigidbody>();
        //rb.AddForce(transform.forward * pred.moveSpeed, ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position += transform.forward * pred.moveSpeed * Time.deltaTime;
    }
}
