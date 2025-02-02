﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prey : MonoBehaviour, ILandAnimal
{
    public Rigidbody rb;

    // EXPERIMENTAL
    [SerializeField] private Vector3 currVelocity = new Vector3();
    // get position of "forefeet" to check for sharply raised terrain
    //[SerializeField] private Vector3 foreFeet = new Vector3();
    // get position of jaw and jaw's offset from the Predator's position
    [SerializeField] private Vector3 jawPointOffset = new Vector3();
    [SerializeField] private Vector3 jawPoint = new Vector3();
    // get previous position of the Prey in world space
    [SerializeField] private Vector3 prevPosition = new Vector3();
    // length of the Ray to evaluate terrain (could place in own function?)
    [SerializeField] private float rayLength = 0.0f;
    // calculated speed of the Rigidbody
    [SerializeField] private float speed = 0.0f;
    // decreased velocity increments (m/s^2), with minimum of 0
    [SerializeField] private float speedDown = 0.0f;
    // calculated max turning radius for the current velocity
    [SerializeField] private float turnRadius = 180.0f;
    // calculated viewpoint and viewpoint offset from the Prey's position
    [SerializeField] private Vector3 localViewPoint = new Vector3();
    [SerializeField] private Vector3 globalViewPoint = new Vector3();

    // the type of land animal (Predator or Prey)
    private string type;

    // binocFOV is binocular field of vision, in degrees
    public float binocFOV = 60.0f;

    // EXPERIMENTAL
    // break force to calculate deceleration and turning radius
    public float breakForce = 900.0f;

    // maximum run speed in m/s
    public float fleeSpeed = 20.0f;
    // the maximum optimum distance of vision
    public float depthPerception = 25.0f;
    // mimics energy expenditure and affects possible move modes, efficiency, and termination of scenario
    public float energy = 0.0f;
    // maximum time will continue evading a lost predator (using last known location/velocity) : EXPERIMENT
    public float lostTimeLimit = 30.0f;

    // EXPERIMENTAL
    // max initial velocity (m/s) from a standing jump
    public float maxStandJump = 7.672027f;

    // mass in kg
    public float pMass = 94.0f;

    // monocFOV is monocular field of vision, in degrees
    public float monocFOV = 125.0f;
    // normal move speed in m/s
    public float moveSpeed = 4.470389f;

    // EXPERIMENTAL
    // scale multiples applied to model's transform
    public Vector3 pSize = new Vector3(0.81f, 0.81f, 0.81f);

    // time needed to rest after sprint
    public float restTimeLimit = 20.0f;
    public float watchTimeLimit = 50.0f;

    // safe fall distance in meters
    public float safeFall = 15.0f;
    // increased velocity increments (m/s^2), with chase speed as limit
    public float speedUp = 9.0f;

    // time limit to a sprint : EXPERIMENT
    public float sprintTimeLimit = 10.0f;

    // max speed when "Resting"
    public float tiredMove = 2.0f;

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        SetTypeAnimal();
        rb = GetComponent<Rigidbody>();
        // set Transform Scale values (relative to parent) equal to object's pSize values
        transform.localScale.Set(pSize.x, pSize.y, pSize.z);

        // FLAT GROUND AND EXACT CENTER MASS ONLY
        // set Transform Position y-value to half of Scale y-value
        transform.position.Set(0.0f, pSize.y * 0.5f, 0.0f);
        //transform.position.Set(0.0f, pSize.y * 0.5f + 1.0f, 0.0f);

        //prevPosition = transform.position;
        // TESTING ONLY
        prevPosition = rb.position;

        // set viewpoint for Raycasting, simulating environmental awareness/FOV
        // viewPointOffset = (FindDeepChild(transform, "eyelid_up.L").position + 
        // FindDeepChild(transform, "eyelid_up.R").position) * 0.5f - rb.position;
        localViewPoint = new Vector3(0.67f, 0.8f, 0.0f);
        globalViewPoint = transform.TransformPoint(localViewPoint);
        //viewPoint = rb.position + viewPointOffset;
        // set position of "forefeet" to navigate sharply raised terrain
        // foreFeet.Set(transform.position.x, transform.position.y - (pSize.y * 0.5f), transform.position.z + (pSize.z * 0.5f));
        // set jaw point for determining epsilon distance
        // jawPointOffset = FindDeepChild(transform, "mouth").position - rb.position;
        jawPointOffset = new Vector3(0.0f, 1.36f, 1.27f);
        jawPoint = rb.position + jawPointOffset;

        // set Rigidbody mass equal to object's mass
        rb.mass = pMass;

        // calculate maximum deceleration value (m/s^2)
        speedDown = -breakForce / pMass;
    }

    public Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        currVelocity = rb.position - prevPosition;
        speed = currVelocity.magnitude / Time.fixedDeltaTime;
        prevPosition = rb.position;
        turnRadius = MaxTurn();
        globalViewPoint = rb.position + localViewPoint;
        jawPoint = rb.position + jawPointOffset;
    }

    public Vector3[] GetGlobalBodyPositions()
    {
        Vector3[] bodyPos = {globalViewPoint, jawPoint};
        return bodyPos;
    }

    public Vector3[] GetLocalBodyPositions()
    {
        Vector3[] bodyPos = {localViewPoint, jawPoint};
        return bodyPos;
    }

    ///*
    // public Vector3[] GetBodyPositions()
    // {
    //     Vector3[] bodyPos = {viewPoint, jawPoint};
    //     return bodyPos;
    // }
    //*/

    public Vector3 GetPrevPosition()
    {
        return prevPosition;
    }

    public float GetSpeed()
    {
        return speed;
    }

    public float GetSpeedDown()
    {
        return speedDown;
    }

    public string GetTypeAnimal()
    {
        return type;
    }

    public Vector3 GetVelocity()
    {
        return currVelocity;
    }

    // calculate the max turning radius given current velocity
    // returns degrees
    public float MaxTurn()
    {
        if (speed == 0.0f)
            return 180.0f;

        float turn = breakForce / (pMass * speed);

        /*
        Debug.Log("breakforce: " + breakForce);
        Debug.Log("pMass: " + pMass);
        Debug.Log("speed: " + speed);
        Debug.Log("turn (rads): " + turn);
        Debug.Log("turn (degs): " + turn * Mathf.Rad2Deg);
        */

        if ((turn * Mathf.Rad2Deg) > 180.0f)
            return 180.0f;
        else
            return turn * Mathf.Rad2Deg;
    }

    public void SetTypeAnimal()
    {
        // set the object's class name (Predator or Prey)
        type = this.GetType().Name;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
