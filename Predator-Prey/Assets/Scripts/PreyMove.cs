using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyMove : MonoBehaviour
{
    // TESTING ONLY
    private Vector3 targetMovePos = new Vector3(-10.0f, 0.5f, 10f);

    // Enumerate the Prey movement states
    // *** need to determine how to handle jumps ***
    public enum PreyStates
    {
        Rest,
        Idle,
        Wander,
        Reck,
        Hide,
        Evade
    };

    public Animator anim;
    public Prey prey;
    public Rigidbody rb;
    // used for target predator
    public Rigidbody pred;
    // used for the sight trigger colliders
    public SphereCollider tColl;

    private bool isJumping = false;
    // current moving state of the Prey
    private PreyStates pmState;

    // used to store the obstacle mask
    private int obstacleMask;
    // used to store the predator mask
    private int predMask;

    // TESTING ONLY
    private int dummyMask;
    // public Rigidbody dummy;

    // get speed value of the Prey
    public float currSpeed = 0.0f;

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        prey = GetComponent<Prey>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        tColl = GetComponent<SphereCollider>();

        obstacleMask = LayerMask.NameToLayer("layer_Obstacle");
        predMask = LayerMask.NameToLayer("layer_Pred");

        // TESTING ONLY
        dummyMask = LayerMask.NameToLayer("layer_Dummy Target");
    }

    // isWatchful parameter includes Reck and Hide states
    bool CheckFOV(Rigidbody other, bool isWatchful)
    {
        // Vector3 toPredator = rb.position - other.position;
        Vector3 toPredator = other.position - prey.GetBodyPositions()[0];

        float angleToPredator = Vector3.Angle(toPredator, transform.right);

        Debug.Log("angleToPredator: " + angleToPredator);
        Debug.Log("hunting FOV limit to each side: " + (prey.binocFOV * 0.5f + (isWatchful ? prey.monocFOV : 0.0f)));
        if(angleToPredator < (prey.binocFOV * 0.5f + (isWatchful ? prey.monocFOV : 0.0f)))
        {
            RaycastHit hit;

            if(Physics.Raycast(prey.GetBodyPositions()[0], toPredator.normalized, out hit, tColl.radius, 1 << dummyMask))
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
        currSpeed = prey.GetSpeed();

        // set speed for Animator
        anim.SetFloat("animSpeed", currSpeed);

        // JUMP TEST     
        /*
        if(!isJumping)
        {
            isJumping = true;
            Jump(new Vector3(0.0f, Mathf.Sin(45.0f * Mathf.Deg2Rad), Mathf.Cos(45.0f * Mathf.Deg2Rad)));
        } */

        if (pmState == PreyStates.Rest)
        {
            // recover energy

            // add logic for all other possible state transitions
        }
        else if (pmState == PreyStates.Idle)
        {
            // default start mode; recovers energy with alertness triggers?

            // add logic for all other possible state transitions
        }
        else if (pmState == PreyStates.Wander)
        {
            // move mode with ?

            // add logic for all other possible state transitions
        }
        else if (pmState == PreyStates.Reck)
        {
            // rb.MovePosition(rb.position + transform.forward * prey.moveSpeed * Time.fixedDeltaTime);

            // SPECIFIC TO DEER MODEL
            rb.MovePosition(rb.position + transform.right * prey.moveSpeed * Time.fixedDeltaTime);

            // seeks position while attempting to keep cover
            if (!isJumping)
            {
                //Flee(targetMovePos);
            }
        }
        else if (pmState == PreyStates.Evade)
        {
            Vector3 predFuturePos;
            
            // SPECIFIC TO DEER MODEL
            rb.MovePosition(rb.position + transform.right * prey.moveSpeed * Time.fixedDeltaTime);

            // CHANGE to Rigidbody for prey
            DummyTarget dt = pred.GetComponent<DummyTarget>();
            if(dt.maxSpeed == 0.0f)
            {
                predFuturePos = pred.transform.position;
            }
            else
                predFuturePos = pred.transform.position + dt.currSpeed * ((pred.transform.position - rb.position) / dt.maxSpeed);

            Flee(predFuturePos);
            Debug.Log("distance is " + (pred.transform.position - rb.position).magnitude);

            if (!pred.gameObject.activeSelf)
            {
                pmState = PreyStates.Reck;
                Debug.Log("Back to reck...");
            }
        }
    }

    public void Jump(Vector3 normalVec)
    {
        // set Animator to proper state
        anim.SetTrigger("jumpTrigger");
        anim.SetBool("isJumping", true);

        prey.rb.velocity = (normalVec * prey.maxStandJump);
        anim.SetBool("isJumping", false);
    }

    private void OnTriggerEnter(Collider other)
    {

    }

    // implement behavior to flee from a target position
    public void Flee(Vector3 target)
    {
        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // calculate current velocity (m/s)
        Vector3 curVelocity = (prey.rb.position - prey.GetPrevPosition()) / Time.fixedDeltaTime;
        // Debug.Log("current velocity is " + curVelocity);

        // desired straight-line velocity to ascape
        Vector3 desVelocity = Vector3.Normalize(rb.position - target) * prey.moveSpeed;

        // check if zero velocity (not already moving)
        if (curVelocity == Vector3.zero)
        {
            Debug.Log("Velocity is zero!");
            changeAngle = Vector3.SignedAngle(prey.transform.right, desVelocity, prey.transform.up);
        }
        else
        {
            // angle change to go directly to desired target
            changeAngle = Vector3.SignedAngle(curVelocity, desVelocity, prey.transform.up);
        }

        // default angle to rotate
        float useAngle = changeAngle;

        // check to see if angle exceeds max
        if (changeAngle > 0.0f)
        {
            if (prey.maxTurn() < changeAngle)
                useAngle = prey.maxTurn();
        }
        else
        {
            if (prey.maxTurn() < Mathf.Abs(changeAngle))
                useAngle = -prey.maxTurn();
        }

        // Debug.Log("Angle between destination and new vectors is " + changeAngle);
        // Debug.Log("Calculated max turn is " + prey.maxTurn());
        // Debug.Log("Using angle " + useAngle);

        rb.MoveRotation(prey.rb.rotation * Quaternion.AngleAxis(useAngle, prey.transform.up));
    }

    // Start is called before the first frame update
    void Start()
    {
        tColl.center = prey.GetBodyPositions()[0];
        tColl.radius = prey.depthPerception;
        // Set initial state to an idle Prey
        // pmState = PreyStates.Idle;

        //TESTING ONLY
        pmState = PreyStates.Reck;

        // Look for existing targets only 
        Collider[] inRange = Physics.OverlapSphere(tColl.center, tColl.radius, 1 << dummyMask);

        Debug.Log("inRange length is " + inRange.Length);

        if (inRange.Length > 0 && pmState == PreyStates.Reck)
        {
            Rigidbody potentPredator = inRange[0].attachedRigidbody;

            if (potentPredator && CheckFOV(potentPredator, true))
            {
                pmState = PreyStates.Evade;
                pred = potentPredator;
                Debug.Log("Predator is located at " + pred.transform.position);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
