using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Predator : MonoBehaviour, ILandAnimal
{
    Rigidbody rb;

    // EXPERIMENTAL
    // calculated speed of the Rigidbody
    [SerializeField] private float speed = 0.0f;
    // decreased velocity increments (m/s^2), with minimum of 0
    [SerializeField] private float speedDown = 0.0f;
    // calculated max turning radius for the current velocity
    [SerializeField] private float turnRadius = 360.0f;
    // calculated view point of the Predator
    [SerializeField] private Vector3 viewPoint = new Vector3();
    
    // the type of land animal (Predator or Prey)
    private string type;

    // binocFOV is binocular field of vision, in degrees
    public float binocFOV = 130.0f;

    // EXPERIMENTAL
    // break force to calculate deceleration and turning radius
    public float breakForce = 750.0f;

    // maximum run speed in m/s
    public float chaseSpeed = 22.0f;
    // the maximum optimum distance of vision
    public float depthPerception = 25.0f;
    // mimics energy expenditure and affects possible move modes, efficiency, and termination of scenario
    public float energy = 0.0f;
    // distance in meters from a target for an automatic kill
    public float epsilon = 1.0f;

    // EXPERIMENTAL
    // initial velocity (m/s) from a standing jump
    public float jumpVelocity = 7.672027f;

    // mass in kg
    public float mass = 65.0f;
    // monocFOV is monocular field of vision, in degrees
    public float monocFOV = 78.5f;
    // normal move speed in m/s
    public float moveSpeed = 2.777777f;

    // EXPERIMENTAL
    // scale multiples applied to model's transform
    public Vector3 size = new Vector3(0.5f, 0.675f, 1.28f);
    
    // increased velocity increments (m/s^2), with chase speed as limit
    public float speedUp = 9.0f;

    // calculate the max turning radius given current velocity
    // returns degrees
    public float maxTurn()
    {
        if (speed == 0.0f)
            return 360.0f;

        float turn = (breakForce * Time.fixedDeltaTime) / (mass * speed);

        if ((turn * Mathf.Rad2Deg) > 360.0f)
            return 360.0f;
        else
            return turn * Mathf.Rad2Deg;
    }

    public string getType()
    {
        return type;
    }

    public void setType()
    {
        // set the object's class name (Predator or Prey)
        type = this.GetType().Name;
    }

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        setType();
        rb = GetComponent<Rigidbody>();
        // set Transform Scale values (relative to parent) equal to object's size values
        transform.localScale.Set(size.x, size.y, size.z);

        // FLAT GROUND AND EXACT CENTER MASS ONLY
        // set Transform Position y-value to half of Scale y-value
        transform.position.Set(0.0f, size.y * 0.5f, 0.0f);

        // set Rigidbody mass equal to object's mass
        rb.mass = mass;

        // calculate maximum deceleration value (m/s^2)
        speedDown = -breakForce / mass;

        // calculate the point from where RayCasts are made, simulating vision
    }

    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {
        speed = rb.velocity.z;
        turnRadius = maxTurn();
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
