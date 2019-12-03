using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyMove : MonoBehaviour, ILAMove
{
    [SerializeField] private Vector3 targetMovePos;

    // Enumerate the Prey movement states
    public enum PreyStates
    {
        //Rest,
        Idle,
        Wander,
        Watch,
        Hide,
        Evade,
        Caught
    };

    public Animator anim;
    public Prey prey;
    // used for target predator
    public Rigidbody pred;
    public Rigidbody rb;
    // used for the sight trigger colliders
    public SphereCollider tColl;
    // awareness of all Prey in sight
    [SerializeField] private List<Rigidbody> awaredPred = new List<Rigidbody>();
    // "unseen" predator/prey within sphere of vision
    [SerializeField] private List<Rigidbody> unseenPred = new List<Rigidbody>();

    // Distance to predator, to determine the speed to flee
    public float criticalDist;
    public float relievingDist;

    // get speed value of the Prey
    private float currSpeed = 0.0f;

    // status boolean 
    [SerializeField] private bool isWatchful = false;
    [SerializeField] private bool isEscaping = false;
    [SerializeField] private bool isJumping = false;
    [SerializeField] private bool isCaught = false;
    [SerializeField] private bool lostPred = false;

    // the last known location and velocity of Predator (whether lost or not)
    private Vector3 lastPredLocation;
    private Vector3 lastPredVelocity;
    private Vector3 lostTargetPos;

    // Timers
    // time since predator was lost
    private float lostPredTime = 0.0f;
    // time resting
    private float restTime = 0.0f;
    // time watchful
    private float watchTime = 0.0f;
    // time sprinting
    private float sprintTime = 0.0f;
    //public float sprintDistLimit = 2.0f;
    // time idling
    private float idleTime = 0.0f;
    // time wandering
    private float wanderTime = 0.0f;
    // random idle limit
    private float randomIdleLimit = 0.0f;
    // time wandering
    private float randomWanderLimit = 0.0f;

    // current moving state of the Prey
    private PreyStates pmState;

    // used to store the predator layermask
    private int predMask;
    // used to store the prey layermask
    private int preyMask;

    // ====== RANDOM MOVE FOR WANDER STATE =======
    // initial slow distance when in Wander mode; EXPERIMENTAL
    //private float slowDist;
    // limits for going to random points
    // [SerializeField] private float watchTime = 0.0f;
    readonly private float xLeftLimit = 35.0f;
    readonly private float xRightLimit = 250.0f;
    readonly private float zFrontLimit = 25.0f;
    readonly private float zBackLimit = 200.0f;
    readonly private float safeRayHeight = 60.0f;
    readonly private float rayLength = 100.0f;

    // original spawn point in case can't find another suitable location
    readonly private Vector3 spawnLoc;

    // max number of tries to attempt choosing target
    // before a default is chosen (avoid infinite loop)
    readonly int maxTries = 5;
    private int tryCount = 1;

    public void Accelerate(float desSpeed)
    {
        float maxAccel = desSpeed - currSpeed;

        if (maxAccel > prey.speedUp)
            maxAccel = prey.speedUp;

        // Debug.Log("Prey acceleration value is " + maxAccel);

        // physical equation using acceleration (per physics frame)
        rb.MovePosition(rb.position + transform.forward * (currSpeed + 0.5f * maxAccel * Time.fixedDeltaTime) * Time.fixedDeltaTime);
    }

    public bool addToAwared(Rigidbody other)
    {
        bool gotPred = false;

        if (!pred)
        {
            pred = other;
            gotPred = true;
        }
        
        if (!awaredPred.Contains(other))
            awaredPred.Add(other);

        if (unseenPred.Contains(other))
            unseenPred.Remove(other);

        Debug.Log("PREDATOR SPOTTED!!!");

        return gotPred;
    }

    public bool AddToUnseen(Rigidbody other)
    {
        bool lost = false;

        if (pred == other)
        {
            PredLastVectors();
            pred = null;

            if (awaredPred.Contains(other))
                awaredPred.Remove(other);

            if (awaredPred.Count > 0)
            {
                pred = awaredPred[0];
            }
            else
                lost = true;
        }

        if (!unseenPred.Contains(other))
            unseenPred.Add(other);

        Debug.Log("PREDATOR LOST!!!");

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

        predMask = LayerMask.NameToLayer("layer_Predator");
        preyMask = LayerMask.NameToLayer("layer_Prey");
    }

    public bool CheckFOV(Rigidbody other, bool isWatchful)
    {
        Debug.Log("Checking FOV");
        float angleToTarget = DegAngleToTarget(other.position, prey.GetGlobalBodyPositions()[0]);

        // Debug.Log("angleToPredator: " + angleToPredator);
        // Debug.Log("Prey: watching FOV limit to each side: " + (prey.binocFOV * 0.5f + (isWatchful ? prey.monocFOV : 0.0f)));
        if (angleToTarget < (prey.binocFOV * 0.5f + (isWatchful ? prey.monocFOV : 0.0f)))
        //if ( angleToTarget < (prey.binocFOV * 0.5f + (isWatchful ? prey.monocFOV : 0.0f)) && pred.GetComponent<Predator>().GetSpeed() > 1.0E-6 )
        {
            RaycastHit hit;

            // only worry about the pred
            if (Physics.Raycast(prey.GetGlobalBodyPositions()[0], DirToTarget(other.position, prey.GetGlobalBodyPositions()[0]).normalized,
                out hit, tColl.radius, 1 << predMask))
            {
                Debug.DrawLine(hit.point, rb.position);
                if (hit.collider.attachedRigidbody == other && !hit.collider.isTrigger)
                {
                    Debug.Log("Prey: returned true in FOV");
                    return true;
                }
                else
                {
                    Debug.Log("Prey: hit " + hit.rigidbody.gameObject.name);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// check whether a predaor is Lost, and Check the Time has been passed since predator lost
    /// used primarily for sphere collider OnTriggerExit
    /// </summary>
    bool CheckLost(Collider other)
    {
        // don't do anything if Collider is a trigger or has no attached Rigidbody
        if (other.isTrigger || !other.attachedRigidbody)
            return false;
        
        if (pred && other.attachedRigidbody == pred)
        {
            PredLastVectors();
            pred = null;
            lostPred = true;
            // can assume prey is not unseen and is in aware list
            awaredPred.Remove(pred);
            return true;
        }

        // predator was not the lost animal
        return false;
    }

    public bool CheckLostTime()
    {
        if (lostPredTime > prey.lostTimeLimit)
        {
            ResetLost();
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

    public bool CheckWatchTime()
    {
        if (watchTime >= prey.watchTimeLimit)
        {
            //ResetWatch();
            return true;
        }

        return false;
    }

    public bool CheckSprintTime()
    {
        //if ((sprintTime >= prey.sprintTimeLimit) || ((targetMovePos - rb.position).magnitude <= sprintDistLimit))
        if (sprintTime >= prey.sprintTimeLimit)
        {
            ResetSprint();
            return true;
        }

        return false;
    }

    public bool CheckIdleTime()
    {
        if (idleTime >= randomIdleLimit)
        {
            ResetIdle();
            //randomWanderLimit = prey.restTimeLimit - randomIdleLimit;
            randomWanderLimit = 10;
            return true;
        }

        return false;
    }

    public bool CheckWanderTime()
    {
        if (wanderTime >= randomWanderLimit)
        {
            ResetWander();
            //randomIdleLimit = prey.restTimeLimit - randomWanderLimit;
            randomIdleLimit = 10;
            return true;
        }

        return false;
    }

    // checks for presence of predator
    public void CheckTrigger(Collider other, bool isWatchful)
    {
        // do nothing if there is no rigidbody
        if (!other.attachedRigidbody)
            return;

        Rigidbody potentPred = other.attachedRigidbody;
        if (potentPred) Debug.Log("PotentPred SPOTTED!!!!!!");
        else Debug.Log("NO PotentPred");

        bool seen = CheckFOV(potentPred, isWatchful);

        if (seen)
        {
            if (addToAwared(potentPred))
            {
                // take action against the seen predator
                // pmState = PredatorMove.Evade;
                isEscaping = true;
                ResetLost();
                PredLastVectors();
            }
        }
    }

    bool ChooseTarget()
    {
        bool foundTarget = false;
        float xCoord = Random.Range(xLeftLimit, xRightLimit);
        float zCoord = Random.Range(zFrontLimit, zBackLimit);

        // targetMovePos = new Vector3(68.0f, 0.7f, 150.0f);

        Ray ray = new Ray(new Vector3(xCoord, safeRayHeight, zCoord), Vector3.down);
        RaycastHit hit;
        TerrainCollider tc = Terrain.activeTerrain.GetComponent<TerrainCollider>();

        while (tryCount <= maxTries && !foundTarget)
        {
            if (tc.Raycast(ray, out hit, rayLength))
            {
                targetMovePos = hit.point;
                foundTarget = true;
            }
            else
            {
                Debug.Log("PreyMove ChooseTarget try # " + tryCount + " at " + hit.point);
                tryCount++;
            }
        }

        Debug.Log("Found a spot!");
        return foundTarget;
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

    // Calculations 
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


    // ------- Needed ???
    // primarily meant for decision to go to Pursue state from Stalk state
    // OnTriggerStay() is the primary mechanism to decide this
    // public void Chase(Collider other)
    // {
    //     Debug.Log("On the chase!");

    //     if (other.attachedRigidbody == prey && !other.isTrigger)
    //         pmState = PredStates.Pursue;
    // }

    // isWatchful parameter includes Watch and states


    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        if (pred)
        {
            if (!CheckFOV(pred, Watchful()))
            {
                if (AddToUnseen(pred))
                    lostPred = true;
            }
        }

        if (unseenPred.Count > 0)
        {
            if (CheckFOV(unseenPred[0], Watchful()))
            {
                if (addToAwared(unseenPred[0]))
                    lostPred = false;
            }
        }

        currSpeed = prey.GetSpeed();
        Debug.Log("Prey's speed is now " + currSpeed);
        Debug.Log("Prey's state : " + pmState);

        // set speed for Animator
        anim.SetFloat("animSpeed", currSpeed);

        // begin to Evade first predator awared of, if any
        // if (awaredPred.Count > 0)
        //     pred = awaredPred[0];
        
        // Update Predator Vectors
        if (pred)
            PredLastVectors();

        //Check Predator Lost
        if (lostPred)
        {
            if (CheckLostTime())
            {
                isEscaping = false;
                if (!ChooseTarget())
                    targetMovePos = spawnLoc;
            }
            else
            {
                lostPredTime += Time.fixedDeltaTime;
                //lostTargetPos = PredictLostLocation();
                targetMovePos = PredictLostLocation();
                // predicted predator movement would leave the environment
                // abort the chase
                if (targetMovePos.x < xLeftLimit || targetMovePos.x > xRightLimit ||
                    targetMovePos.z < zFrontLimit || targetMovePos.z > zBackLimit)
                {
                    if (!ChooseTarget())
                        targetMovePos = spawnLoc;
                }
            }
        }

        // Check Rest Time
        if ((pmState == PreyStates.Idle) || (pmState == PreyStates.Wander)) {
            if (CheckRestTime())
            {
                isWatchful = true;
                //slowDist = (targetMovePos - rb.position).magnitude;
                //pmState = PreyStates.Watch;
            }
            else
            {
                restTime += Time.fixedDeltaTime;
            }
        }
        

        // Check Watch Time
        if ((pmState == PreyStates.Watch) || (pmState == PreyStates.Evade)) {
            if (CheckWatchTime() && lostPred)
            {
                ResetWatch();
                isWatchful = false;
                //slowDist = (targetMovePos - rb.position).magnitude;
                //pmState = PreyStates.Wander;
            }
            else
            {
                watchTime += Time.fixedDeltaTime;
            }   
        }

        // Move
        if (pmState == PreyStates.Wander)
        {
            if (!ChooseTarget())
                targetMovePos = spawnLoc;
        }

        // JUMP TEST     
        /*
        if(!isJumping)
        {
            isJumping = true;
            Jump(new Vector3(0.0f, Mathf.Sin(45.0f * Mathf.Deg2Rad), Mathf.Cos(45.0f * Mathf.Deg2Rad)));
        } */

        // State Behavior
        if (pmState == PreyStates.Idle)
        {
            if(isEscaping)
            {
                 pmState = PreyStates.Evade;
            }
            else if(isWatchful)
            {
                pmState = PreyStates.Watch;
            }

            // Check Idle Time
            if (CheckIdleTime())
            {
                pmState = PreyStates.Wander;
            }
            else
            {
                idleTime += Time.fixedDeltaTime;
            }
        }

        else if (pmState == PreyStates.Wander)
        {
            if(isEscaping)
            {
                 pmState = PreyStates.Evade;
            }
            else if(isWatchful)
            {
                pmState = PreyStates.Watch;
            }

            // Check Wander Time
            if (CheckWanderTime())
            {
                pmState = PreyStates.Idle;
            }
            else
            {
                wanderTime += Time.fixedDeltaTime;
                Flee(prey.moveSpeed, targetMovePos, false);
            }
            
        }

        else if (pmState == PreyStates.Watch)
        {
            if(isEscaping){
                 pmState = PreyStates.Evade;
            }
            else if (!isWatchful) {
                pmState = PreyStates.Wander;
            }
                
            //rb.MovePosition(rb.position + transform.forward * prey.moveSpeed * Time.fixedDeltaTime);
            // seeks position while attempting to keep cover
            // Flee(prey.moveSpeed, targetMovePos);
        }

        else if (pmState == PreyStates.Evade)
        {
            Predator predStats;
            Vector3 predFuturePos;
            float predFutureDist;
            float medianSpeed;

            if (!isEscaping)
                pmState = PreyStates.Watch;

            if (pred)
            {
                predStats = pred.GetComponent<Predator>();

                if (predStats.chaseSpeed == 0.0f || predStats.GetSpeed() == 0.0f)
                {
                    predFuturePos = pred.position;
                }
                else
                {
                    predFuturePos = GetFuturePos(pred.position, predStats.GetVelocity(), true);
                }
                predFutureDist = DirToTarget(predFuturePos, rb.position).magnitude;

                if (!CheckSprintTime())
                {
                    if( predFutureDist < criticalDist ) 
                    {
                        //currSpeed = (currSpeed > prey.fleeSpeed)?  prey.fleeSpeed : currSpeed + prey.speedUp * Time.fixedDeltaTime;
                        //Accelerate(prey.fleeSpeed);
                        Flee(prey.fleeSpeed, predFuturePos, true);
                
                    }
                    else if ( predFutureDist > relievingDist ) 
                    {
                        //currSpeed = (currSpeed < prey.moveSpeed)?  prey.moveSpeed : currSpeed - prey.speedUp * Time.fixedDeltaTime;
                        //Decelerate(prey.moveSpeed);
                        Flee(prey.moveSpeed, predFuturePos, false);
                    }
                    else 
                    {
                        Flee(currSpeed, predFuturePos, false);
                    }
                }
                else 
                {
                    medianSpeed = (prey.moveSpeed + prey.fleeSpeed)/2;
                    //Decelerate(medianSpeed);
                    Flee(currSpeed, predFuturePos, true);
                }
            }
            else if (!pred || lostPred)
            {
                lostTargetPos = PredictLostLocation();
                predFutureDist = DirToTarget(lostTargetPos, rb.position).magnitude;

                if (!CheckSprintTime())
                {
                    if( predFutureDist < criticalDist ) 
                    {
                        Flee(prey.fleeSpeed, lostTargetPos, true);
                
                    }
                    else if ( predFutureDist > relievingDist ) 
                    {
                        Flee(prey.moveSpeed, lostTargetPos, false);
                    }
                    else 
                    {
                        Flee(currSpeed, lostTargetPos, false);
                    }
                }
                else 
                {
                    medianSpeed = (prey.moveSpeed + prey.fleeSpeed)/2;
                    Flee(currSpeed, lostTargetPos, false);
                }
            }

        }
    }

    public void FullScan()
    {
        Collider[] inRange = Physics.OverlapSphere(prey.GetGlobalBodyPositions()[0], tColl.radius, 1 << preyMask);

        Debug.Log("Prey: inRange length is " + inRange.Length);

        if (inRange.Length == 0)
            return;

        foreach (Collider c in inRange)
        {
            if (inRange.Length > 0 && Watchful())
            {
                CheckTrigger(c, true);
                Debug.Log("Prey: inRange collider here ");
            }
        }
    }

    public Vector3 GetFuturePos(Vector3 pos, Vector3 vel, bool useDeltaTime)
    {
        return pos + vel * (useDeltaTime ? Time.fixedDeltaTime : 1.0f);
    }

    public Vector3 GetLostPrediction()
    {
        return lastPredLocation + lastPredVelocity * lostPredTime;
    }

    // public float GetSlowDistance(Vector3 targetPos)
    // {
    //     return -(currSpeed * currSpeed / (prey.GetSpeedDown() * 2.0f));
    // }

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
        CheckTrigger(other, Watchful());
    }

    private void OnTriggerExit(Collider other)
    {
        CheckLost(other);
    }
    
    public Vector3 PredictLostLocation()
    {
        return lastPredLocation + lastPredVelocity * lostPredTime;
    }

    public void RemoveFromLists(Rigidbody gone)
    {
        if (awaredPred.Contains(gone))
            awaredPred.Remove(gone);

        if (unseenPred.Contains(gone))
            unseenPred.Remove(gone);

        if (gone == pred)
        {
            pred = null;

            if (awaredPred.Count > 0)
                pred = awaredPred[0];
        }
    }


    // implement behavior to flee from a target position
    public void Flee(float maxSpeed, Vector3 target, bool isCritical)
    {
        float turn = prey.MaxTurn();

        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // calculate current velocity (m/s)
        Vector3 currVelocity = prey.GetVelocity().normalized;
        // Debug.Log("current velocity is " + curVelocity);

        // desired straight-line velocity to ascape
        Vector3 futurePos = GetFuturePos(rb.position, currVelocity * currSpeed, false);

        //float futureAngleToTarget = DegAngleToTarget(target, rb.position);
        float futureAngleToTarget = DegAngleToTarget(rb.position, target);
        // Debug.Log("current velocity is " + curVelocity);

        // move in desired straight-line velocity to target
        AdjSpeedForAngle(maxSpeed, futureAngleToTarget);

        // check if zero velocity (not already moving)
        if (currVelocity == Vector3.zero)
        {
            Debug.Log("Velocity is zero!");
            //changeAngle = Vector3.SignedAngle(prey.transform.forward, desVelocity, prey.transform.up);
            changeAngle = Vector3.SignedAngle(prey.transform.forward, DirToTarget(rb.position, target), prey.transform.up);
        }
        else
        {
            // angle change to go directly to desired target
            //changeAngle = Vector3.SignedAngle(currVelocity, desVelocity, prey.transform.up);
            changeAngle = Vector3.SignedAngle(currVelocity, DirToTarget(futurePos, target), prey.transform.up);
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

        useAngle *= Time.fixedDeltaTime;

        // Debug.Log("Angle between destination and new vectors is " + changeAngle);
        // Debug.Log("Calculated max turn is " + prey.maxTurn());
        // Debug.Log("Using angle " + useAngle);

        rb.MoveRotation(prey.rb.rotation * Quaternion.AngleAxis(useAngle, prey.transform.up));
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

    public void ResetWatch()
    {
        watchTime = 0.0f;
    }

    public void ResetSprint()
    {
        sprintTime = 0.0f;
    }

    public void ResetIdle()
    {
        idleTime = 0.0f;
    }

    public void ResetWander()
    {
        wanderTime = 0.0f;
    }

    public void Seek(float maxSpeed, Vector3 target)
    {
 
    }

    // bool SlowToTarget(Vector3 targetPos, float avgSpeed)
    // {
    //     return slowDist <= -(currSpeed * currSpeed) / (prey.GetSpeedDown() * 2.0f);
    // }

    // Start is called before the first frame update
    void Start()
    {
        tColl.center = prey.GetLocalBodyPositions()[0];
        tColl.radius = prey.depthPerception;
        // Set initial state to an idle Prey
        // pmState = PreyStates.Idle;

        // TESTING ONLY; GET NEW TARGET IF GO HERE
        //slowDist = (targetMovePos - rb.position).magnitude;
        if (!ChooseTarget())
            targetMovePos = spawnLoc;
        pmState = PreyStates.Wander;
        randomWanderLimit = Random.Range(2.0f, 12.0f);

        // Look for existing targets only
        FullScan();
    }

    // Update is called once per frame
    void Update()
    {

    }


    /// <summary>
    /// function for Lost Pred
    /// </summary>
    public void PredLastVectors()
    {
        Predator predStats = pred.GetComponent<Predator>();

        lastPredLocation = pred.position;
        lastPredVelocity = predStats.GetVelocity();

        // Debug.Log("Predator is located at " + lastPredLocation);
        // Debug.Log("Predator's velocity is " + lastPredVelocity);
    }

    public bool Watchful()
    {
        if ((pmState == PreyStates.Evade) || (pmState == PreyStates.Watch))
            return true;

        return false;
    }
}
