using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Predator : MonoBehaviour, ILandAnimal
{
    // the type of land animal (Predator or Prey)
    private string type;
    // binocFOV is binocular field of vision, in degrees
    public float binocFOV = 130.0f;
    // maximum run speed in m/s
    public float chaseSpeed = 22.0f;
    // the maximum optimum distance of vision
    public float depthPerception = 25.0f;
    // mimics energy expenditure and affects possible move modes, efficiency, and termination of scenario
    public float energy = 0.0f;
    // distance in meters from a target for an automatic kill
    public float epsilon = 1.0f;
    // mass in kg
    public float mass = 65.0f;
    // monocFOV is monocular field of vision, in degrees
    public float monocFOV = 78.5f;
    // normal move speed in m/s
    public float moveSpeed = 2.777777f;
    // scale multiples applied to model
    public Vector3 size;
    // top acceleration value is in seconds; how quickly the predator can reach chase speed
    //public float topAcceleration;
    //public float turningRadius;
    // public enum moveMode {};

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
    }

    // FixedUpdate is called a number of times based upon current frame rate
    // All physics calculations and updates occur immediately after FixedUpdate
    // Do not need to multiply values by Time.deltaTime
    void FixedUpdate()
    {

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
