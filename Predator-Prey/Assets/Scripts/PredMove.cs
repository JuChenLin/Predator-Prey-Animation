using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredMove : MonoBehaviour, ILAMove
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

    // used to store the obstacle layermask
    private int obstacleMask;
    // used to store the prey layermask
    private int preyMask;

    // TESTING ONLY
    private int dummyMask;
    // public Rigidbody dummy;

    // get speed value of the Predator
    private float currSpeed = 0.0f;

    public void Accelerate(float desSpeed)
    {
        float maxAccel = desSpeed - currSpeed;

        if (maxAccel > pred.speedUp)
            maxAccel = pred.speedUp;

         maxAccel *= Time.fixedDeltaTime;

        Debug.Log("acceleration value is " + maxAccel);

        rb.MovePosition(rb.position + transform.right * (currSpeed + maxAccel) * Time.fixedDeltaTime);
    }

    public void AdjSpeedForAngle(float maxSpeed, float desAngle)
    {
        float optSpeed = maxSpeed;

        // avoid overshooting the target
        if (desAngle > (pred.MaxTurn() / Time.fixedDeltaTime))
        {
            Debug.Log("desired angle is greater than max turn!");
            optSpeed = (pred.breakForce * Time.fixedDeltaTime) / ((desAngle * Mathf.Deg2Rad) * rb.mass);
        }

        if (optSpeed > maxSpeed)
            optSpeed = maxSpeed;
        else if (optSpeed < 0.0f)
            optSpeed = 0.0f;

        Debug.Log("calculated optimal speed is " + optSpeed);

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

        // TESTING ONLY
        dummyMask = LayerMask.NameToLayer("layer_Dummy Target");
    }

    // isHunting parameter includes Hunt and Stalk states
    public bool CheckFOV(Rigidbody other, bool isHunting)
    {
        float angleToPrey = DegAngleToTarget(other.position);

        Debug.Log("hunting FOV limit to each side: " + (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)));
        if(angleToPrey < (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)))
        {
            RaycastHit hit;

            if(Physics.Raycast(pred.GetBodyPositions()[0], DirToTarget(other.position).normalized, out hit, tColl.radius, 1 << dummyMask))
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

    public void Decelerate(float desSpeed)
    {
        float maxDecel = currSpeed - desSpeed;

        if (maxDecel > pred.GetSpeedDown())
            maxDecel = pred.GetSpeedDown();

        maxDecel *= Time.fixedDeltaTime;

        Debug.Log("deceleration value is " + maxDecel);

        rb.MovePosition(rb.position + transform.right * (currSpeed + maxDecel) * Time.fixedDeltaTime);
    }

    public float DegAngleToTarget(Vector3 target)
    {
        // Vector3 toTarget = rb.position - target;
        Vector3 toTarget = DirToTarget(target);

        float angleToTarget = Vector3.Angle(toTarget, transform.right);
        Debug.Log("angleToTarget: " + angleToTarget);

        return angleToTarget;
    }

    public Vector3 DirToTarget(Vector3 target)
    {
        return target - pred.GetBodyPositions()[0];
    }

    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        currSpeed = pred.GetSpeed();
        Debug.Log("speed is now " + currSpeed);

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

            // seeks position while attempting to keep cover
            if (!isJumping)
            {
                Seek(pred.moveSpeed, huntMovePos);
            }
        }
        else if (pmState == PredatorMove.Pursue)
        {
            Vector3 preyFuturePos;

            // float maxAccel = ((currSpeed + pred.speedUp) < pred.chaseSpeed) ? pred.speedUp : (pred.chaseSpeed - currSpeed);
            
            // SPECIFIC TO COUGAR MODEL
            // rb.MovePosition(rb.position + transform.right * (currSpeed + maxAccel) * Time.fixedDeltaTime);

            // CHANGE to Rigidbody for prey
            DummyTarget dt = prey.GetComponent<DummyTarget>();
            if (dt.maxSpeed == 0.0f)
            {
                preyFuturePos = prey.position;
            }
            else
            {
                preyFuturePos = prey.position + dt.currSpeed * ((prey.position - rb.position) / dt.maxSpeed);
                // preyFuturePos = prey.position + dt.currSpeed * Time.fixedDeltaTime * ((prey.position - rb.position) / dt.maxSpeed);
            }

            Seek(pred.chaseSpeed, preyFuturePos);
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

    public void Seek(float maxSpeed, Vector3 target)
    {
        float angleToTarget = DegAngleToTarget(target);

        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // calculate current velocity (m/s)
        // Vector3 curVelocity = pred.GetVelocity() / Time.fixedDeltaTime;
        Vector3 curVelocity = pred.GetVelocity().normalized;
        
        // Debug.Log("current velocity is " + curVelocity);

        // move in desired straight-line velocity to target
        AdjSpeedForAngle(maxSpeed, angleToTarget);

        // check if zero velocity (not already moving)
        if (curVelocity == Vector3.zero)
        {
            Debug.Log("Velocity is zero!");
            changeAngle = Vector3.SignedAngle(pred.transform.right, DirToTarget(target), pred.transform.up);
        }
        else
        {
            // angle change to go directly to desired target
            changeAngle = Vector3.SignedAngle(curVelocity, DirToTarget(target), pred.transform.up);
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

        // SPECIFIC TO COUGAR MODEL
        // rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

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
