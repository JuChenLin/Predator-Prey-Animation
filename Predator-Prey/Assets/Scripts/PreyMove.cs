using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyMove : MonoBehaviour, ILAMove
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
    // used for target predator
    public Rigidbody pred;
    public Prey prey;
    public Rigidbody rb;
    // used for the sight trigger colliders
    public SphereCollider tColl;
    // awareness of all Prey in sight
    [SerializeField] private List<Rigidbody> preyAware = new List<Rigidbody>();
    // "unseen" predator/prey within sphere of vision
    [SerializeField] private List<Rigidbody> unseen = new List<Rigidbody>();

    // get speed value of the Prey
    private float currSpeed = 0.0f;
    private bool isJumping = false;

    // the last known location and velocity of Predator (whether lost or not)
    private Vector3 lastPredLocation;
    private Vector3 lastPredVelocity;

    // is predator lost? JUST CHECK pred!
    private bool lostPred = false;
    // time since predator was lost
    private float lostPredTime = 0.0f;
    // time resting
    private float restTime = 0.0f;
    // time sprinting
    private float sprintTime = 0.0f;

    // current moving state of the Prey
    private PreyStates pmState;

    // used to store the obstacle layermask
    private int obstacleMask;
    // used to store the predator layermask
    private int predMask;
    // used to store the prey layermask
    private int preyMask;

    public void Accelerate(float desSpeed)
    {
        float maxAccel = desSpeed - currSpeed;

        if (maxAccel > prey.speedUp)
            maxAccel = prey.speedUp;

        // Debug.Log("Prey acceleration value is " + maxAccel);

        // physical equation using acceleration (per physics frame)
        rb.MovePosition(rb.position + transform.forward * (currSpeed + 0.5f * maxAccel * Time.fixedDeltaTime) * Time.fixedDeltaTime);
    }

    public bool addToAware(Rigidbody other)
    {
        bool gotPred = false;

        if (other.tag == "tag_Predator")
        {
            if (!pred)
            {
                pred = other;
                gotPred = true;
            }

            if (unseen.Contains(other))
                unseen.Remove(other);
        }
        else if (other.tag == "tag_Prey")
        {
            if (!preyAware.Contains(other))
                preyAware.Add(other);

            if (unseen.Contains(other))
                unseen.Remove(other);
        }

        return gotPred;
    }

    public bool addToUnseen(Rigidbody other)
    {
        Predator predStats = prey.GetComponent<Predator>();
        bool lost = false;

        lastPredLocation = pred.position;
        lastPredVelocity = predStats.GetVelocity();

        if (pred == other)
        {
            pred = null;
            lost = true;
        }

        if (preyAware.Contains(other))
            preyAware.Remove(other);

        if (!unseen.Contains(other))
            unseen.Add(other);

        return lost;
    }

    public void AdjSpeedForAngle(float maxSpeed, float desAngle)
    {
        float optSpeed = maxSpeed;

        // avoid overshooting the target
        if (desAngle > prey.MaxTurn())
        {
            // Debug.Log("Prey: desired angle is greater than max turn!");
            optSpeed = prey.breakForce / ((desAngle * Mathf.Deg2Rad) * rb.mass);
        }

        if (optSpeed > maxSpeed)
            optSpeed = maxSpeed;
        else if (optSpeed < 0.0f)
            optSpeed = 0.0f;

        // Debug.Log("Prey's calculated optimal speed is " + optSpeed);

        if (optSpeed < currSpeed)
            Decelerate(optSpeed);
        else
            Accelerate(optSpeed);
    }

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        prey = GetComponent<Prey>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        tColl = GetComponent<SphereCollider>();

        anim.runtimeAnimatorController = Resources.Load("Animator Controllers/ac_Prey") as RuntimeAnimatorController;

        obstacleMask = LayerMask.NameToLayer("layer_Obstacle");
        predMask = LayerMask.NameToLayer("layer_Pred");
        preyMask = LayerMask.NameToLayer("layer_Prey");

        /*
        // TESTING ONLY
        dummyMask = LayerMask.NameToLayer("layer_Dummy Target");
        */
    }

    // isWatchful parameter includes Reck and Hide states
    public bool CheckFOV(Rigidbody other, bool isWatchful)
    {
        float angleToTarget = DegAngleToTarget(other.position, prey.GetBodyPositions()[0]);

        // Debug.Log("angleToPredator: " + angleToPredator);
        // Debug.Log("Prey: watching FOV limit to each side: " + (prey.binocFOV * 0.5f + (isWatchful ? prey.monocFOV : 0.0f)));
        if (angleToTarget < (prey.binocFOV * 0.5f + (isWatchful ? prey.monocFOV : 0.0f)))
        {
            RaycastHit hit;

            // only worry about the predator or prey
            if (Physics.Raycast(prey.GetBodyPositions()[0], DirToTarget(other.position, prey.GetBodyPositions()[0]).normalized,
                out hit, tColl.radius, (1 << predMask) | (1 << preyMask)))
            {
                if (hit.collider.attachedRigidbody == other)
                {
                    Debug.Log("Prey: hit " + hit.rigidbody.gameObject.name);
                    return true;
                }
                else
                {
                    // Debug.Log("Prey: hit " + hit.rigidbody.gameObject.name);
                }
            }
        }

        return false;
    }

    // this function assumes any past awareness of predator and other prey has already been established
    // used primarily for sphere collider OnTriggerExit
    bool CheckLost(Collider other)
    {
        // don't do anything if predator hasn't already been detected
        // or if Collider is a trigger
        if (!pred || other.isTrigger)
            return false;

        if (addToUnseen(other.attachedRigidbody))
        {
            Debug.Log("ADD LOST PROTOCOL HERE, OR HANDLE IN FIXED UPDATE");
        }

        return true;
    }

    public bool CheckLostTime()
    {
        if (lostPredTime > prey.lostTimeLimit)
        {
            ResetLost();
            if (pmState != PreyStates.Rest)
                pmState = PreyStates.Wander;
            return true;
        }
        return false;
    }

    public bool CheckRestTime()
    {
        if (restTime >= prey.restTimeLimit)
        {
            ResetRest();
            return true;
        }

        return false;
    }

    public bool CheckSprintTime()
    {
        if (sprintTime >= prey.sprintTimeLimit)
        {
            ResetSprint();
            return true;
        }

        return false;
    }

    // checks for presence of predator or other prey
    public void CheckTrigger(Collider other, bool isWatchful)
    {
        Rigidbody potentPred = other.attachedRigidbody;

        // do nothing if there is no rigidbody
        if (!potentPred)
            return;

        string rbType = potentPred.tag;
        bool seen = CheckFOV(potentPred, isWatchful);

        if (seen)
        {
            if (addToAware(potentPred))
            {
                // take action against the seen predator
                // pmState = PredatorMove.Evade;
                pmState = PreyStates.Hide;
                lastPredLocation = pred.position;
                // Debug.Log("Predator is located at " + lastPredLocation);
            }
        }
    }

    public void Decelerate(float desSpeed)
    {
        float maxDecel = currSpeed - desSpeed;

        if (maxDecel > prey.GetSpeedDown())
            maxDecel = prey.GetSpeedDown();

        // Debug.Log("Prey's deceleration value is " + maxDecel);

        // physical equation using deceleration (per physics frame)
        rb.MovePosition(rb.position + transform.forward * (currSpeed + 0.5f * maxDecel * Time.fixedDeltaTime) * Time.fixedDeltaTime);
    }

    public float DegAngleToTarget(Vector3 target, Vector3 startPos)
    {
        // Vector3 toTarget = rb.position - target;
        Vector3 toTarget = DirToTarget(target, startPos);

        float angleToTarget = Vector3.Angle(toTarget, transform.forward);
        // Debug.Log("Prey: angleToTarget: " + angleToTarget);

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
        if (CheckLostTime() && pmState != PreyStates.Rest)
            pmState = PreyStates.Wander;

        currSpeed = prey.GetSpeed();
        // Debug.Log("Prey's speed is now " + currSpeed);

        // set speed for Animator
        anim.SetFloat("animSpeed", currSpeed);

        if (lostPred)
            lostPredTime += Time.fixedDeltaTime;

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
            restTime += Time.fixedDeltaTime;

            if (CheckRestTime())
            {
                ResetRest();

                // continue to be watchful if a predator is known or anticipated
                if (pred || lostPred)
                    pmState = PreyStates.Reck;
                else
                    pmState = PreyStates.Wander;
            }

            // LOGIC TO SEEK AT TIRED SPEED, DEPENDING UPON PREDATOR PRESENCE OR LOST STATE
            // SUCH AS
            // Seek(prey.tiredMove, TARGET), where TARGET is determined as if Reck;
        }
        else if (pmState == PreyStates.Idle)
        {
            // default start mode; recovers energy with alertness triggers?

            // add logic for all other possible state transitions
        }
        else if (pmState == PreyStates.Wander)
        {
            // move mode with more direct routes to targets?

            // add logic for all other possible state transitions
        }
        else if (pmState == PreyStates.Reck)
        {
            //rb.MovePosition(rb.position + transform.forward * prey.moveSpeed * Time.fixedDeltaTime);

            // seeks position while attempting to keep cover
            if (!isJumping)
            {
                // Flee(prey.moveSpeed, targetMovePos);
            }
        }
        else if (pmState == PreyStates.Evade)
        {
            Vector3 predFuturePos;
            Predator predStats;

            sprintTime += Time.fixedDeltaTime;

            if (pred)
            {
                predStats = pred.GetComponent<Predator>();

                if (predStats.chaseSpeed == 0.0f)
                {
                    predFuturePos = pred.position;
                }
                else
                {
                    // predFuturePos = pred.position + predStats.GetSpeed() * (DirToTarget(pred.position, rb.position) / predStats.chaseSpeed);
                    // predFuturePos = pred.position + predStats.GetVelocity() * Time.fixedDeltaTime;
                    predFuturePos = GetFuturePos(pred.position, predStats.GetVelocity(), true);
                }

                Flee(prey.fleeSpeed, predFuturePos);
            }

            // Debug.Log("distance to Predator is " + (DirToTarget(prey.position, rb.position)).magnitude);

            if(CheckSprintTime())
                pmState = PreyStates.Rest;
        }
    }

    public void FullScan()
    {
        Collider[] inRange = Physics.OverlapSphere(tColl.center, tColl.radius, 1 << preyMask);

        Debug.Log("Prey: inRange length is " + inRange.Length);

        if (inRange.Length == 0)
            return;

        foreach (Collider c in inRange)
        {
            if (inRange.Length > 0 && Watchful())
            {
                CheckTrigger(c, true);
            }
        }
    }

    public Vector3 GetFuturePos(Vector3 pos, Vector3 vel, bool useDeltaTime)
    {
        return pos + vel * (useDeltaTime ? Time.fixedDeltaTime : 1.0f);
    }

    public void Jump(Vector3 normalVec)
    {
        // set Animator to proper state
        anim.SetTrigger("jumpTrigger");
        anim.SetBool("isJumping", true);

        prey.rb.velocity = (normalVec * prey.maxStandJump);

        // ACTUALLY DO THIS WHEN KNOW TRANSITIONED TO ANOTHER STATE
        anim.SetBool("isJumping", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckTrigger(other, pmState == PreyStates.Reck);
    }

    // implement behavior to flee from a target position
    public void Flee(float maxSpeed, Vector3 target)
    {
        // SEE SEEK BELOW, AS IT HAS BEEN MODIFIED
        // SEEK CAN BE USED FOR FLEE, JUST A DIFFERENT TARGET, AS PURSUE STATE DOES

        /*
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
            changeAngle = Vector3.SignedAngle(prey.transform.forward, desVelocity, prey.transform.up);
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
            if (prey.MaxTurn() < changeAngle)
                useAngle = prey.MaxTurn();
        }
        else
        {
            if (prey.MaxTurn() < Mathf.Abs(changeAngle))
                useAngle = -prey.MaxTurn();
        }

        // Debug.Log("Angle between destination and new vectors is " + changeAngle);
        // Debug.Log("Calculated max turn is " + prey.maxTurn());
        // Debug.Log("Using angle " + useAngle);

        rb.MoveRotation(prey.rb.rotation * Quaternion.AngleAxis(useAngle, prey.transform.up));
        */
    }

    public void ResetLost()
    {
        lostPredTime = 0.0f;
        lostPred = false;
    }

    public void ResetRest()
    {
        restTime = 0.0f;
    }

    public void ResetSprint()
    {
        sprintTime = 0.0f;
    }

    public void Seek(float maxSpeed, Vector3 target)
    {
        float turn = prey.MaxTurn();

        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // calculate current velocity (m/s)
        Vector3 currVelocity = prey.GetVelocity().normalized;
        // anticipated future position based upon current velocity
        // Vector3 futurePos = rb.position + currVelocity * currSpeed;
        Vector3 futurePos = GetFuturePos(rb.position, currVelocity * currSpeed, false);

        float futureAngleToTarget = DegAngleToTarget(target, futurePos);

        // Debug.Log("current velocity is " + curVelocity);

        // move in desired straight-line velocity to target
        AdjSpeedForAngle(maxSpeed, futureAngleToTarget);

        // check if zero velocity (not already moving)
        if (currVelocity == Vector3.zero)
        {
            Debug.Log("Pred: Velocity is zero!");
            changeAngle = Vector3.SignedAngle(prey.transform.forward, DirToTarget(target, rb.position), prey.transform.up);
        }
        else
        {
            // angle change to go directly to desired target
            changeAngle = Vector3.SignedAngle(currVelocity, DirToTarget(target, futurePos), prey.transform.up);
            // changeAngle = Vector3.SignedAngle(prey.transform.forward, DirToTarget(target), prey.transform.up);
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

        // SPECIFIC TO DEER MODEL
        // rb.MovePosition(rb.position + transform.forward * prey.moveSpeed * Time.fixedDeltaTime);

        // Debug.Log("Prey's max turn angle is " + turn);
        // Debug.Log("Prey's moving turn angle is " + useAngle);
        rb.MoveRotation(prey.rb.rotation * Quaternion.AngleAxis(useAngle, prey.transform.up));
    }

    // Start is called before the first frame update
    void Start()
    {
        tColl.center = prey.GetBodyPositions()[0];
        tColl.radius = prey.depthPerception;
        // Set initial state to an idle Prey
        // pmState = PreyStates.Idle;

        // TESTING ONLY
        pmState = PreyStates.Wander;

        // Look for existing targets only 
        FullScan();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public bool Watchful()
    {
        if ((pmState == PreyStates.Evade) || (pmState == PreyStates.Hide) || (pmState == PreyStates.Reck))
            return true;

        return false;
    }
}
