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

    public Animator anim;
    public Predator pred;
    public Rigidbody rb;
    // used for target prey
    public Rigidbody prey;
    // used for the sight trigger collider
    public SphereCollider tColl;

    private bool isJumping = false;
    // current moving state of the Predator
    private PredatorMove pmState;

    // used to store the obstacle mask
    private int obstacleMask;
    // used to store the prey mask
    private int preyMask;

    // TESTING ONLY
    private int dummyMask;
    // public Rigidbody dummy;

    // get speed value of the Predator
    private float currSpeed = 0.0f;

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        pred = GetComponent<Predator>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        tColl = GetComponent<SphereCollider>();

        obstacleMask = LayerMask.NameToLayer("layer_Obstacle");
        preyMask = LayerMask.NameToLayer("layer_Prey");

        // TESTING ONLY
        dummyMask = LayerMask.NameToLayer("layer_Dummy Target");
    }

    // isHunting parameter includes Hunt and Stalk states
    bool CheckFOV(Rigidbody other, bool isHunting)
    {
        // Vector3 toPrey = rb.position - other.position;
        Vector3 toPrey = other.position - pred.GetBodyPositions()[0];

        float angleToPrey = Vector3.Angle(toPrey, transform.right);

        Debug.Log("angleToPrey: " + angleToPrey);
        Debug.Log("hunting FOV limit to each side: " + (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)));
        if(angleToPrey < (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)))
        {
            RaycastHit hit;

            if(Physics.Raycast(pred.GetBodyPositions()[0], toPrey.normalized, out hit, tColl.radius, 1 << dummyMask))
            {     
                if (hit.collider.attachedRigidbody == other)
                {
                    Debug.Log("returned true in FOV");
                    return true;
                }
                else
                {
                    Debug.Log("hit " + hit.rigidbody.gameObject.name);
                }
            }
        }

        return false;
    }

    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        currSpeed = pred.GetSpeed();

        // set speed for Animator
        anim.SetFloat("animSpeed", currSpeed);

        // JUMP TEST     
        /*
        if(!isJumping)
        {
            isJumping = true;
            Jump(new Vector3(0.0f, Mathf.Sin(45.0f * Mathf.Deg2Rad), Mathf.Cos(45.0f * Mathf.Deg2Rad)));
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
            rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

            // seeks position while attempting to keep cover
            if (!isJumping)
            {
                // Seek(huntMovePos);
            }
        }
        else if (pmState == PredatorMove.Pursue)
        {
            Vector3 preyFuturePos;
            
            // SPECIFIC TO COUGAR MODEL
            rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

            // CHANGE to Rigidbody for prey
            DummyTarget dt = prey.GetComponent<DummyTarget>();
            
            if(dt.maxSpeed == 0.0f)
            {
                preyFuturePos = prey.transform.position;
            }
            else
                preyFuturePos = prey.transform.position + dt.currSpeed * ((prey.transform.position - rb.position) / dt.maxSpeed);

            Seek(preyFuturePos);
            Debug.Log("distance is " + (prey.transform.position - rb.position).magnitude);

            if (!prey.gameObject.activeSelf)
            {
                pmState = PredatorMove.Hunt;
                Debug.Log("Back to hunt...");
            }
        }
    }

    public void Jump(Vector3 normalVec)
    {
        // set Animator to proper state
        anim.SetTrigger("jumpTrigger");
        anim.SetBool("isJumping", true);

        pred.rb.velocity = (normalVec * pred.maxStandJump);
        anim.SetBool("isJumping", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    // implement behavior to seek a target position
    public void Seek(Vector3 target)
    {
        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // calculate current velocity (m/s)
        Vector3 curVelocity = (pred.rb.position - pred.GetPrevPosition()) / Time.fixedDeltaTime;
        // Debug.Log("current velocity is " + curVelocity);

        // desired straight-line velocity to target
        Vector3 desVelocity = Vector3.Normalize(target - rb.position) *
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
            if (pred.MaxTurn() < changeAngle)
                useAngle = pred.MaxTurn();
        }
        else
        {
            if (pred.MaxTurn() < Mathf.Abs(changeAngle))
                useAngle = -pred.MaxTurn();
        }

        // Debug.Log("Angle between destination and new vectors is " + changeAngle);
        // Debug.Log("Calculated max turn is " + pred.maxTurn());
        // Debug.Log("Using angle " + useAngle);

        rb.MoveRotation(pred.rb.rotation * Quaternion.AngleAxis(useAngle, pred.transform.up));
    }

    // Start is called before the first frame update
    void Start()
    {
        tColl.center = pred.GetBodyPositions()[0];
        tColl.radius = pred.depthPerception;
        // Set initial state to an idle Predator
        //pmState = PredatorMove.Idle;

        // TESTING ONLY
        pmState = PredatorMove.Hunt;

        // Look for existing targets only 
        Collider[] inRange = Physics.OverlapSphere(tColl.center, tColl.radius, 1 << dummyMask);

        Debug.Log("inRange length is " + inRange.Length);

        if (inRange.Length > 0 && pmState == PredatorMove.Hunt)
        {
            Rigidbody potentPrey = inRange[0].attachedRigidbody;

            if (potentPrey && CheckFOV(potentPrey, true))
            {
                pmState = PredatorMove.Pursue;
                prey = potentPrey;
                Debug.Log("Prey is located at " + prey.transform.position);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
