using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredMove : MonoBehaviour, ILAMove
{
    // TESTING ONLY
    private Vector3 targetMovePos = new Vector3(-10.0f, 0.5f, 10f);

    // Enumerate the Predator movement states
    // *** need to determine how to handle jumps ***
    public enum PredStates
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
    // used for target prey
    public Rigidbody prey;
    public Rigidbody rb;
    // used for the sight trigger collider
    public SphereCollider tColl;

    // get speed value of the Predator
    private float currSpeed = 0.0f;
    private bool isJumping = false;

    // the last known location and velocity of stalked, chased, or lost prey
    private Vector3 lastPreyLocation;
    private Vector3 lastPreyVelocity;

    // time since prey was lost
    private float lostPreyTime = 0.0f;
    // time sprinting
    private float sprintTime;

    // current moving state of the Predator
    private PredStates pmState;

    // used to store the obstacle layermask
    private int obstacleMask;
    // used to store the predator layermask
    private int predMask;
    // used to store the prey layermask
    private int preyMask;

    public void Accelerate(float desSpeed)
    {
        float maxAccel = desSpeed - currSpeed;

        if (maxAccel > pred.speedUp)
            maxAccel = pred.speedUp;

        Debug.Log("Predator acceleration value is " + maxAccel);

        // physical equation using acceleration (per physics frame)
        rb.MovePosition(rb.position + transform.right * (currSpeed + 0.5f * maxAccel * Time.fixedDeltaTime) * Time.fixedDeltaTime);
    }

    public void AdjSpeedForAngle(float maxSpeed, float desAngle)
    {
        float optSpeed = maxSpeed;

        // avoid overshooting the target
        if (desAngle > pred.MaxTurn())
        {
            Debug.Log("Predator: desired angle is greater than max turn!");
            optSpeed = pred.breakForce / (desAngle * Mathf.Deg2Rad * rb.mass);
        }

        if (optSpeed > maxSpeed)
            optSpeed = maxSpeed;
        else if (optSpeed < 0.0f)
            optSpeed = 0.0f;

        Debug.Log("Predator's calculated optimal speed is " + optSpeed);

        if (optSpeed < currSpeed)
            Decelerate(optSpeed);
        else
            Accelerate(optSpeed);
    }

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        pred = GetComponent<Predator>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        tColl = GetComponent<SphereCollider>();

        anim.runtimeAnimatorController = Resources.Load("Animator Controllers/ac_Predator") as RuntimeAnimatorController;

        obstacleMask = LayerMask.NameToLayer("layer_Obstacle");
        preyMask = LayerMask.NameToLayer("layer_Prey");

        /*
        // TESTING ONLY
        dummyMask = LayerMask.NameToLayer("layer_Dummy Target");
        */
    }

    // isHunting parameter includes Hunt and Stalk states
    public bool CheckFOV(Rigidbody other, bool isHunting)
    {
        float angleToPrey = DegAngleToTarget(other.position, pred.GetBodyPositions()[0]);

        // Debug.Log("angleToPrey: " + angleToPrey);
        // Debug.Log("Predator: hunting FOV limit to each side: " + (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)));
        if(angleToPrey < (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)))
        {
            RaycastHit hit;

            if(Physics.Raycast(pred.GetBodyPositions()[0], DirToTarget(other.position, pred.GetBodyPositions()[0]).normalized, out hit, tColl.radius, 1 << preyMask))
            {     
                if (hit.collider.attachedRigidbody == other)
                {
                    Debug.Log("Predator: returned true in FOV");
                    return true;
                }
                else
                {
                    Debug.Log("Predator: hit " + hit.rigidbody.gameObject.name);
                }
            }
        }

        return false;
    }

    public void CheckPreyEaten()
    {
        if (!prey.gameObject.activeSelf)
        {
            prey = null;
            pmState = PredStates.Hunt;
            anim.SetBool("isStalking", false);
            Debug.Log("Back to hunt...");
        }
    }

    public void CheckTrigger(Collider other, bool isHunting)
    {
        Rigidbody potentPrey = other.attachedRigidbody;

        if (potentPrey && CheckFOV(potentPrey, isHunting))
        {
            pmState = PredStates.Pursue;
            //pmState = PredStates.Stalk;
            prey = potentPrey;
            lastPreyLocation = prey.position;
            Debug.Log("Prey is located at " + lastPreyLocation);
        }
    }

    public void Decelerate(float desSpeed)
    {
        float maxDecel = currSpeed - desSpeed;

        if (maxDecel > pred.GetSpeedDown())
            maxDecel = pred.GetSpeedDown();

        Debug.Log("Predator's deceleration value is " + maxDecel);

        // physical equation using deceleration (per physics frame)
        rb.MovePosition(rb.position + transform.right * (currSpeed + 0.5f * maxDecel * Time.fixedDeltaTime) * Time.fixedDeltaTime);
    }

    public float DegAngleToTarget(Vector3 target, Vector3 startPos)
    {
        // Vector3 toTarget = rb.position - target;
        Vector3 toTarget = DirToTarget(target, startPos);

        float angleToTarget = Vector3.Angle(toTarget, transform.right);
        Debug.Log("Predator: angleToTarget: " + angleToTarget);

        return angleToTarget;
    }

    public Vector3 DirToTarget(Vector3 target, Vector3 startPos)
    {
        return target - startPos;
    }

    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        currSpeed = pred.GetSpeed();
        Debug.Log("Predator's speed is now " + currSpeed);

        // set speed for Animator
        anim.SetFloat("animSpeed", currSpeed);

        // JUMP TEST     
        /*
        if(!isJumping)
        {
            isJumping = true;
            Jump(new Vector3(0.0f, Mathf.Sin(45.0f * Mathf.Deg2Rad), Mathf.Cos(45.0f * Mathf.Deg2Rad)));
        } */

        if (pmState == PredStates.Rest)
        {
            // recover energy

            // add logic for all other possible state transitions
        }
        else if (pmState == PredStates.Idle)
        {
            // default start mode; recovers energy with alertness triggers?

            // add logic for all other possible state transitions
        }
        else if (pmState == PredStates.Wander)
        {
            // move mode with more direct routes to targets?

            // add logic for all other possible state transitions
        }
        else if (pmState == PredStates.Hunt)
        {
            // rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

            // seeks position while attempting to keep cover
            if (!isJumping)
            {
                Seek(pred.moveSpeed, targetMovePos);
            }
        }
        else if (pmState == PredStates.Pursue)
        {
            Vector3 preyFuturePos;

            Prey preyStats = prey.GetComponent<Prey>();

            if (preyStats.fleeSpeed == 0.0f || preyStats.GetSpeed() == 0.0f)
            {
                preyFuturePos = prey.position;
            }
            else
            {
                // preyFuturePos = prey.position + preyStats.GetSpeed() * (DirToTarget(prey.position, rb.position) / preyStats.fleeSpeed);
                preyFuturePos = prey.position + preyStats.GetVelocity() * Time.fixedDeltaTime;
            }

            Seek(pred.chaseSpeed, preyFuturePos);
            Debug.Log("distance to Prey is " + (DirToTarget(prey.position, rb.position)).magnitude);

            CheckPreyEaten();
        }
        else if (pmState == PredStates.Stalk)
        {
            Vector3 preyPos;
            float stalk = pred.stalkSpeed;

            anim.SetBool("isStalking", true);
            preyPos = prey.position;
            Seek(pred.stalkSpeed, preyPos);
            CheckPreyEaten();
        }
    }

    public void Jump(Vector3 normalVec)
    {
        // set Animator to proper state
        anim.SetTrigger("jumpTrigger");
        anim.SetBool("isJumping", true);

        pred.rb.velocity = (normalVec * pred.maxStandJump);

        // ACTUALLY DO THIS WHEN KNOW TRANSITIONED TO ANOTHER STATE
        anim.SetBool("isJumping", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckTrigger(other, pmState == PredStates.Hunt);
    }

    private void OnTriggerExit(Collider other)
    {
        
    }

    public void Seek(float maxSpeed, Vector3 target)
    {
        float turn = pred.MaxTurn();

        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // calculate current velocity (m/s)
        Vector3 currVelocity = pred.GetVelocity().normalized;
        // anticipated future position based upon current velocity
        Vector3 futurePos = rb.position + currVelocity * currSpeed;

        float futureAngleToTarget = DegAngleToTarget(target, futurePos);

        // Debug.Log("current velocity is " + curVelocity);

        // move in desired straight-line velocity to target
        AdjSpeedForAngle(maxSpeed, futureAngleToTarget);

        // check if zero velocity (not already moving)
        if (currVelocity == Vector3.zero)
        {
            Debug.Log("Velocity is zero!");
            changeAngle = Vector3.SignedAngle(pred.transform.right, DirToTarget(target, rb.position), pred.transform.up);
        }
        else
        {
            // angle change to go directly to desired target
            changeAngle = Vector3.SignedAngle(currVelocity, DirToTarget(target, futurePos), pred.transform.up);
            // changeAngle = Vector3.SignedAngle(pred.transform.right, DirToTarget(target), pred.transform.up);
        }

        // default angle to rotate
        float useAngle = changeAngle;

        // check to see if angle exceeds max
        if (changeAngle > 0.0f)
        {
            if (turn < changeAngle)
                useAngle = turn;
        }
        else
        {
            if (turn < Mathf.Abs(changeAngle))
                useAngle = -turn;
        }

        useAngle *= Time.fixedDeltaTime;

        // Debug.Log("Angle between destination and new vectors is " + changeAngle);
        // Debug.Log("Calculated max turn is " + pred.maxTurn());
        // Debug.Log("Using angle " + useAngle);

        // SPECIFIC TO COUGAR MODEL
        // rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

        Debug.Log("Predator's max turn angle is " + turn);
        Debug.Log("Predator's moving turn angle is " + useAngle);
        rb.MoveRotation(pred.rb.rotation * Quaternion.AngleAxis(useAngle, pred.transform.up));
    }

    // Start is called before the first frame update
    void Start()
    {
        tColl.center = pred.GetBodyPositions()[0];
        tColl.radius = pred.depthPerception;
        // Set initial state to an idle Predator
        // pmState = PredStates.Idle;

        // TESTING ONLY
        pmState = PredStates.Hunt;

        // Look for existing targets only 
        Collider[] inRange = Physics.OverlapSphere(tColl.center, tColl.radius, 1 << preyMask);

        Debug.Log("Predator: inRange length is " + inRange.Length);

        if (inRange.Length > 0 && pmState == PredStates.Hunt)
        {
            CheckTrigger(inRange[0], true);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
