﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredMove : MonoBehaviour, ILAMove
{
    public Vector3 targetMovePos;

    // Enumerate the Predator movement states
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
    // awareness of other Prey in sight
    [SerializeField] private List<Rigidbody> preyAware = new List<Rigidbody>();
    // "unseen" prey within sphere of vision
    [SerializeField] private List<Rigidbody> unseen = new List<Rigidbody>();

    // get speed value of the Predator
    private float currSpeed = 0.0f;
    private bool isJumping = false;

    // the last known location and velocity of stalked, chased, or lost prey
    private Vector3 lastPreyLocation;
    private Vector3 lastPreyVelocity;

    // is all potential prey lost?
    private bool lostPrey = false;
    // time since prey was lost
    private float lostPreyTime = 0.0f;
    // time resting
    private float restTime = 0.0f;
    // time sprinting
    private float sprintTime = 0.0f;

    // max jump distance at 45-degree angle
    float maxJumpDist;

    // initial slow distance when in Hunt mode; EXPERIMENTAL
    private float slowDist;

    // current moving state of the Predator
    private PredStates pmState;

    // used to store the obstacle layermask
    private int obstacleMask;
    // used to store the predator layermask
    private int predMask;
    // used to store the prey layermask
    private int preyMask;

    // ====== RANDOM MOVE FOR HUNT STATE =======
    // limits for going to random points
    public float seekTimeLimit = 10.0f;
    public float seekDistLimit = 2.0f;
    [SerializeField] private float seekTime = 0.0f;
    readonly private float xLeftLimit = 35.0f;
    readonly private float xRightLimit = 250.0f;
    readonly private float zFrontLimit = 25.0f;
    readonly private float zBackLimit = 200.0f;
    readonly private float safeRayHeight = 60.0f;
    readonly private float rayLength = 100.0f;

    // original spawn point in case can't find another suitable location
    private Vector3 spawnLoc;

    // max number of tries to attempt choosing target
    // before a default is chosen (avoid infinite loop)
    readonly int maxTries = 5;
    private int tryCount = 1;

    public void Accelerate(float desSpeed)
    {
        float maxAccel = desSpeed - currSpeed;

        if (maxAccel > pred.speedUp)
            maxAccel = pred.speedUp;

        // physical equation using acceleration (per physics frame)
        rb.MovePosition(rb.position + transform.right * (currSpeed + 0.5f * maxAccel * Time.fixedDeltaTime) * Time.fixedDeltaTime);
    }

    public bool AddToAware(Rigidbody other)
    {
        bool gotPrey = false;

        if (!prey)
        {
            prey = other;
            gotPrey = true;
        }

        if (!preyAware.Contains(other))
            preyAware.Add(other);

        if (unseen.Contains(other))
            unseen.Remove(other);

        Debug.Log("PREY SPOTTED!!!");

        return gotPrey;
    }

    // this method assumes prey Rigidbody has already been checked!
    public bool AddToUnseen(Rigidbody other)
    {
        bool lost = false;

        if (prey == other)
        {
            UpdatePreyVectors();
            prey = null;

            if (preyAware.Count > 0)
            {
                prey = preyAware[0];
            }
            else
                lost = true;
        }

        if (preyAware.Contains(other))
            preyAware.Remove(other);

        if (!unseen.Contains(other))
            unseen.Add(other);

        Debug.Log("PREY LOST!!!");

        return lost;
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
    }

    // primarily meant for decision to go to Pursue state from Stalk state
    // OnTriggerStay() is the primary mechanism to decide this
    public void Chase(Collider other)
    {
        /*if (other.attachedRigidbody == prey && !other.isTrigger)
            pmState = PredStates.Pursue; */

        // prey has (likely) left the visible area, so pursue

        pmState = PredStates.Pursue;
    }

    // isHunting parameter includes Hunt and Stalk states
    public bool CheckFOV(Rigidbody other, bool isHunting)
    {
        float angleToTarget = DegAngleToTarget(other.position, pred.GetGlobalBodyPositions()[0]);

        // Debug.Log("angleToPrey: " + angleToPrey);
        // Debug.Log("Predator: hunting FOV limit to each side: " + (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)));
        if(angleToTarget < (pred.binocFOV * 0.5f + (isHunting ? pred.monocFOV : 0.0f)))
        {
            RaycastHit hit;

            // only worry about the prey
            if(Physics.Raycast(pred.GetGlobalBodyPositions()[0], DirToTarget(other.position, pred.GetGlobalBodyPositions()[0]).normalized,
                out hit, tColl.radius, 1 << preyMask))
            {     
                if (hit.collider.attachedRigidbody == other && !hit.collider.isTrigger)
                {
                    Debug.Log("Predator: returned true in FOV");
                    return true;
                }
                else
                {
                    Debug.Log("Predator: FOV hit " + hit.rigidbody.gameObject.name);
                }
            }
        }

        return false;
    }

    // used for sphere collider OnTriggerExit
    bool CheckLost(Collider other)
    {
        // don't do anything if other Collider is a trigger or has no attached Rigidbody
        if (other.isTrigger || !other.attachedRigidbody)
            return false;

        if (prey && other.attachedRigidbody == prey)
        {
            UpdatePreyVectors();
            prey = null;
            lostPrey = true;
            // can assume prey is not unseen and is in aware list
            preyAware.Remove(prey);
            return true;
        }

        // remove from both lists because animals out of sight range
        if (preyAware.Contains(other.attachedRigidbody))
            preyAware.Remove(other.attachedRigidbody);

        if (unseen.Contains(other.attachedRigidbody))
            unseen.Remove(other.attachedRigidbody);

        // target prey was not the lost animal
        return false;
    }

    public bool CheckLostTime()
    {
        if (lostPreyTime > pred.lostTimeLimit)
        {
            ResetLost();
            return true;
        }
        return false;
    }

    public void CheckPreyEaten()
    {
        if (prey)
        {
            if (!prey.gameObject.activeSelf)
            {
                preyAware.Remove(prey);
                prey = null;

                if (preyAware.Count > 0)
                    prey = preyAware[0];

                // EXPERIMENTAL
                slowDist = (targetMovePos - rb.position).magnitude;
                pmState = PredStates.Hunt;

                anim.SetBool("isStalking", false);
            }
        }
    }

    public bool CheckRestTime()
    {
        if (restTime >= pred.restTimeLimit)
        {
            ResetRest();
            return true;
        }

        return false;
    }

    public bool CheckSeekLimits()
    {
        if ((seekTime >= seekTimeLimit) || ((targetMovePos - rb.position).magnitude <= seekDistLimit))
        {
            ResetSeekTime();
            return true;
        }

        return false;
    }

    public bool CheckSprintTime()
    {
        if (sprintTime >= pred.sprintTimeLimit)
        {
            ResetSprint();
            return true;
        }

        return false;
    }

    // checks for presence of prey
    public void CheckTrigger(Collider other, bool isHunting)
    {
        // do nothing if there is no rigidbody
        if (!other.attachedRigidbody)
            return;

        Rigidbody potentPrey = other.attachedRigidbody;

        bool seen = CheckFOV(potentPrey, isHunting);

        if (seen)
        {
            // return true if a new prey was found
            if (AddToAware(potentPrey))
            {
                if (pmState == PredStates.Hunt)
                {
                    pmState = PredStates.Stalk;
                }
                UpdatePreyVectors();
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
                Debug.Log("PredMove ChooseTarget try # " + tryCount + " at " + hit.point);
                tryCount++;
            }
        }

        Debug.Log("Found a spot!");
        return foundTarget;
    }

    public void Decelerate(float desSpeed)
    {
        float maxDecel = currSpeed - desSpeed;

        if (maxDecel > pred.GetSpeedDown())
            maxDecel = pred.GetSpeedDown();

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
    void FixedUpdate()
    {
        if (preyAware.Count > 0)
        {
            foreach (Rigidbody p in preyAware)
            {
                if (!CheckFOV(p, Hunting()))
                {
                    if (AddToUnseen(p))
                        lostPrey = true;
                }
            }
        }

        if (unseen.Count > 0)
        {
            foreach (Rigidbody u in unseen)
            {
                if (CheckFOV(u, Hunting()))
                {
                    if (AddToAware(u))
                    {
                        lostPrey = false;

                        if (pmState == PredStates.Hunt)
                            pmState = PredStates.Stalk;
                    }
                }
            }
        }

        if (prey)
            UpdatePreyVectors();

        if (lostPrey)
        {
            if (CheckLostTime())
            {
                if (!ChooseTarget())
                    targetMovePos = spawnLoc;

                pmState = PredStates.Hunt;
            }
            else
            {
                lostPreyTime += Time.fixedDeltaTime;
                targetMovePos = PredictLostLocation();

                // predicted predator movement would leave the environment
                // abort the chase
                if (targetMovePos.x < xLeftLimit || targetMovePos.x > xRightLimit ||
                    targetMovePos.z < zFrontLimit || targetMovePos.z > zBackLimit)
                {
                    if (!ChooseTarget())
                        targetMovePos = spawnLoc;

                    if (pmState == PredStates.Pursue)
                        pmState = PredStates.Hunt;
                }
            }

            if (pmState == PredStates.Stalk)
                pmState = PredStates.Hunt;
        }

        currSpeed = pred.GetSpeed();

        // set speed for Animator
        anim.SetFloat("animSpeed", currSpeed);

        if (pmState == PredStates.Rest)
        {
            if (CheckRestTime())
            {
                // begin to hunt first prey aware of, if any
                if (preyAware.Count > 0)
                    prey = preyAware[0];

                if (!ChooseTarget())
                    targetMovePos = spawnLoc;

                slowDist = (targetMovePos - rb.position).magnitude;
                pmState = PredStates.Hunt;
            }
            else
            {
                // recover energy
                restTime += Time.fixedDeltaTime;
            }

            if (pred.GetSpeed() > 0.0f)
            {
                // come to a halt if tired
                Decelerate(0.0f);
            }
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
            Debug.Log("=========Hunting==========");

            // TEST ONLY
            // rb.MovePosition(rb.position + transform.right * pred.moveSpeed * Time.fixedDeltaTime);

            if(CheckSeekLimits())
            {
                if (!ChooseTarget())
                    targetMovePos = spawnLoc;
            }
            else
            {
                seekTime += Time.fixedDeltaTime;
            }

            if (!isJumping)
            {
                /*
                if (SlowToTarget(pred.moveSpeed))
                    Decelerate(0.0f);
                else
                */

                Seek(pred.moveSpeed, targetMovePos);
            }
            else if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Pounce"))
            {
                // set Animator to proper state
                anim.SetBool("isJumping", false);
                isJumping = false;
            }
        }
        else if (pmState == PredStates.Pursue)
        {
            Debug.Log("^^^^^^^^Pursue^^^^^^^^^");
            Vector3 preyFuturePos;
            Prey preyStats;

            if (!CheckSprintTime())
            {
                sprintTime += Time.fixedDeltaTime;

                if (!isJumping)
                {
                    if (prey)
                    {
                        preyStats = prey.GetComponent<Prey>();

                        if (preyStats.fleeSpeed == 0.0f || preyStats.GetSpeed() == 0.0f)
                        {
                            preyFuturePos = prey.position;
                        }
                        else
                        {
                            // preyFuturePos = prey.position + preyStats.GetSpeed() * (DirToTarget(prey.position, rb.position) / preyStats.fleeSpeed);
                            // preyFuturePos = prey.position + preyStats.GetVelocity() * Time.fixedDeltaTime;
                            preyFuturePos = GetFuturePos(prey.position, preyStats.GetVelocity(), true);
                        }

                        Seek(pred.chaseSpeed, preyFuturePos);

                        Debug.Log("distance to Prey is " + (DirToTarget(prey.position, rb.position)).magnitude);
                    }
                    else
                        Seek(pred.chaseSpeed, GetFuturePos(lastPreyLocation, lastPreyVelocity, true));
                }
                else if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Pounce"))
                {
                    // set Animator to proper state
                    anim.SetBool("isJumping", false);
                    isJumping = false;
                }
            }
            else
            {
                // no longer worried about detected or lost prey
                prey = null;
                ResetLost();
                pmState = PredStates.Rest;
            }
        }
        else if (pmState == PredStates.Stalk)
        {
            Debug.Log(".......Stalking........");

            Vector3 direction = DirToTarget(prey.position, rb.position);

            if (direction.magnitude <= maxJumpDist)
            {
                isJumping = true;
                anim.SetBool("isJumping", true);

                Jump(new Vector3(direction.x, Mathf.Sin(45.0f * Mathf.Deg2Rad) * direction.y,
                    Mathf.Cos(45.0f * Mathf.Deg2Rad) * direction.z).normalized,
                    JumpVelocNeeded(DirToTarget(prey.position, rb.position).magnitude));

                pmState = PredStates.Pursue;
            }

            if (!isJumping)
            {
                anim.SetBool("isStalking", true);
                Seek(pred.stalkSpeed, prey.position);
            }
        }

        CheckPreyEaten();
    }

    public void FullScan()
    {
        Collider[] inRange = Physics.OverlapSphere(pred.GetGlobalBodyPositions()[0], tColl.radius, 1 << preyMask);

        Debug.Log("Predator: inRange length is " + inRange.Length);

        if (inRange.Length == 0)
            return;

        foreach (Collider c in inRange)
        {
            if (inRange.Length > 0 && Hunting())
            {
                if(!c.isTrigger)
                    CheckTrigger(c, true);
            }
        }
    }

    public Vector3 GetFuturePos(Vector3 pos, Vector3 vel, bool useDeltaTime)
    {
        return pos + vel * (useDeltaTime ? Time.fixedDeltaTime : 1.0f);
    }

    public float GetSlowDistance(float avgSpeed)
    {
        return -(avgSpeed * avgSpeed / (pred.GetSpeedDown() * 2.0f));
    }

    public bool Hunting()
    {
        if ((pmState == PredStates.Hunt) || (pmState == PredStates.Stalk))
            return true;

        return false;
    }

    public void Jump(Vector3 normalVec, float initSpeed)
    {
        // set Animator to proper state
        anim.SetBool("isJumping", true);

        pred.rb.velocity = (normalVec * initSpeed);
    }

    public float MaxJumpDistCalc()
    {
        // assumes jump at optimal angle for distance
        return Mathf.Pow(pred.maxStandJump, 2.0f) / 9.81f;
    }

    public float JumpVelocNeeded(float dist)
    {
        return Mathf.Sqrt(dist * 9.81f);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckTrigger(other, Hunting());
    }

    private void OnTriggerExit(Collider other)
    {
        if (CheckLost(other) && pmState == PredStates.Stalk)
        {
            pmState = PredStates.Pursue;
            anim.SetBool("isStalking", false);
        }
    }

    public Vector3 PredictLostLocation()
    {
        return lastPreyLocation + lastPreyVelocity * lostPreyTime;
    }

    public void ResetLost()
    {
        lostPreyTime = 0.0f;
        lostPrey = false;
    }

    public void ResetRest()
    {
        restTime = 0.0f;
    }

    public void ResetSeekTime()
    {
        seekTime = 0.0f;
    }

    public void ResetSprint()
    {
        sprintTime = 0.0f;
    }

    public void Seek(float maxSpeed, Vector3 target)
    {
        float turn = pred.MaxTurn();

        // if not already moving, first get a bearing
        float changeAngle = 0.0f;

        // calculate current velocity (m/s)
        Vector3 currVelocity = pred.GetVelocity().normalized;
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

    bool SlowToTarget(float avgSpeed)
    {
        Debug.Log("distance to target is " + (targetMovePos - rb.position).magnitude);
        Debug.Log("calc distance to slow is " + -(currSpeed * currSpeed / (pred.GetSpeedDown() * 2.0f)));
        return slowDist <= -(avgSpeed * avgSpeed) / (pred.GetSpeedDown() * 2.0f);
    }

    // Start is called before the first frame update
    void Start()
    {
        tColl.center = pred.GetLocalBodyPositions()[0];
        tColl.radius = pred.depthPerception;

        slowDist = (targetMovePos - rb.position).magnitude;
        spawnLoc = rb.position;

        maxJumpDist = MaxJumpDistCalc();

        if (!ChooseTarget())
            targetMovePos = spawnLoc;
        pmState = PredStates.Hunt;

        // Look for existing targets only 
        FullScan();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdatePreyVectors()
    {
        Prey preyStats = prey.GetComponent<Prey>();

        lastPreyLocation = prey.position;
        lastPreyVelocity = preyStats.GetVelocity();
    }
}
