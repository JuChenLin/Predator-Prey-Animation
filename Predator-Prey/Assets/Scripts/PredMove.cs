using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredMove : MonoBehaviour
{
    // TESTING ONLY
    private Vector3 huntMovePos = new Vector3(-10.0f, 0.5f, 10f);

    // Enumerate the Predator movement states
    // *** need to determine how to handle jumps ***
    public enum PredatorMove
    {
        Rest,
        Idle,
        Wander,
        Hunt,
        Stalk,
        Pursue
    };

    public Predator pred;
    public Rigidbody rb;

    private bool isJumping = false;
    // current moving state of the Predator
    private PredatorMove pmState;

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        pred = GetComponent<Predator>();
        rb = GetComponent<Rigidbody>();
    }

        // FixedUpdate is called a number of times based upon current frame rate
        // All physics calculations and updates occur immediately after FixedUpdate
        // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        // rb.MovePosition(rb.position + transform.forward * pred.moveSpeed * Time.fixedDeltaTime);

        // JUMP TEST     
        /*
        if(!isJumping)
        {
           isJumping = true;
           pred.Jump(new Vector3(0.0f, Mathf.Sin(45.0f * Mathf.Deg2Rad), Mathf.Cos(45.0f * Mathf.Deg2Rad)));
        } */

        if (pmState == PredatorMove.Rest)
        {
            // recover energy

            // add logic for all other possible state transitions
        }
        else if (pmState == PredatorMove.Idle)
        {
            // default start mode; recovers energy with alertness triggers?

            // add logic for all other possible state transitions
        }
        else if (pmState == PredatorMove.Wander)
        {
            // move mode with more direct routes to targets?

            // add logic for all other possible state transitions
        }
        else if (pmState == PredatorMove.Hunt)
        {
            // rb.MovePosition(rb.position + transform.forward * pred.moveSpeed * Time.fixedDeltaTime);

            // SPECIFIC TO COUGAR MODEL
            // rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

            // seeks position while attempting to keep cover
            Seek();
        }
    }

    // implement behavior to seek a target position
    void Seek()
    {
        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // SPECIFIC TO COUGAR MODEL
        rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

        // calculate current velocity (m/s)
        Vector3 curVelocity = (pred.rb.position - pred.getPrevPosition()) / Time.fixedDeltaTime;
        // Debug.Log("current velocity is " + curVelocity);

        // desired straight-line velocity to target
        Vector3 desVelocity = Vector3.Normalize(huntMovePos - rb.position) *
            pred.moveSpeed;

        // check if zero velocity (not already moving)
        if (curVelocity == Vector3.zero)
        {
            Debug.Log("Velocity is zero!");
            changeAngle = Vector3.SignedAngle(pred.transform.right, desVelocity, pred.transform.up);
        }
        else
        {
            // angle change to go directly to desired target
            changeAngle = Vector3.SignedAngle(curVelocity, desVelocity, pred.transform.up);
        }

        // default angle to rotate
        float useAngle = changeAngle;

        // check to see if angle exceeds max
        if (changeAngle > 0.0f)
        {
            if (pred.maxTurn() < changeAngle)
                useAngle = pred.maxTurn();
        }
        else
        {
            if (pred.maxTurn() < Mathf.Abs(changeAngle))
                useAngle = -pred.maxTurn();
        }

        Debug.Log("Angle between destination and new vectors is " + changeAngle);
        Debug.Log("Calculated max turn is " + pred.maxTurn());
        Debug.Log("Using angle " + useAngle);
        // Debug.Log("Angle between original and new vectors is " + Vector3.Angle(curVelocity, desVelocity));
        rb.MoveRotation(pred.rb.rotation * Quaternion.AngleAxis(useAngle, pred.transform.up));

        // SPECIFIC TO COUGAR MODEL
        // rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);
    }

    // Start is called before the first frame update
    void Start()
    {
         // Set initial state to an idle Predator
        //pmState = PredatorMove.Idle;

        // TESTING ONLY
        pmState = PredatorMove.Hunt;
    }

    // Update is called once per frame
    void Update()
    {
        
        // transform.position += transform.forward * pred.moveSpeed * Time.deltaTime;

        /* if (pmState == PredatorMove.Rest)
        {
            // recover energy

            // add logic for all other possible state transitions
        }
        else if (pmState == PredatorMove.Idle)
        {
            // default start mode; recovers energy with alertness triggers?

            // add logic for all other possible state transitions
        }
        else if (pmState == PredatorMove.Wander)
        {
            // move mode with more direct routes to targets?

            // add logic for all other possible state transitions
        }
        else if (pmState == PredatorMove.Hunt)
        {
            transform.position += transform.forward * pred.moveSpeed * Time.deltaTime;

            // seeks position or Prey while attempting to keep cover

            // calculate current velocity (m/s)
            Vector3 curVelocity = (pred.transform.position - pred.getPrevPosition()) / Time.deltaTime;

            // desired straight-line velocity to target
            Vector3 desVelocity = Vector3.Normalize(huntMovePos - transform.position) *
                pred.moveSpeed;

            // max angle change to go directly to desired target
            float changeAngle = Vector3.SignedAngle(curVelocity, desVelocity, pred.transform.up);

            // default angle to rotate
            float useAngle = changeAngle;

            // check to see if angle exceeds max
            if (changeAngle > 0.0f)
            {
                if (pred.maxTurn() < changeAngle)
                    useAngle = pred.maxTurn();
            }
            else
            {
                if (pred.maxTurn() < Mathf.Abs(changeAngle))
                    useAngle = -pred.maxTurn();
            }

            Debug.Log("Angle between original and new vectors is " + changeAngle);
            Debug.Log("Using angle " + useAngle);
            // Debug.Log("Angle between original and new vectors is " + Vector3.Angle(curVelocity, desVelocity));
            pred.transform.rotation = Quaternion.AngleAxis(useAngle, pred.transform.up) * pred.transform.rotation;
        } */
    }
}
