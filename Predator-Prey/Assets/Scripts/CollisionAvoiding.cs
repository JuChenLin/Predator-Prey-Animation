using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionAvoiding : MonoBehaviour
{
    // Get reference
    public GameObject player;
    public Rigidbody rb;
    public SphereCollider tColl;

    // Angent info, only for Testing
    public float mass;
    public float breakForce;
    public float moveSpeed;
    // public float moveDirection;
    // public float depthPerception;

    // ======================== Sensing
    // --------------- Obstacles 
    // Adjustable variables
    public float obstAlertDist;
    public float obstAvoidDist;
    public float obstCounterCoefficient;
    // public float safeAngle;
    // public float safeDistance;
    
    // Obstacles sensing
    private RaycastHit obstacleHit;
    private int obstacleMask;
    private Collider[] obstacles;
    private float obstAngle;
    private float obstAngleSign;

    // --------------- Walls
    // Adjustable variables
    public float wallAlertDist;
    public float wallAvoidDist;
    public float wallCounterCoefficient;
    
    // Walls sensing
    private RaycastHit wallHit;
    private int wallMask;
    private Collider[] walls;
    private float[] wallDistance;
    private Vector3[] wallDirection;
    // =================================

    // ======================== Steering
    private float random01Coeficient;
    private int randomSignCoeficient;

    private float counterForce;
    private float counterAcceleration;
    private float maxAngularVel;  // Theta/t, degree/sec
    private float maxRotateAngle;
    private float turnAngle;
    private Vector3 paraDirection;
    // =================================


    
    void Awake()
    {
        // Get reference
        player = GameObject.Find("Player");
        rb = player.GetComponent<Rigidbody>();
        tColl = player.GetComponent<SphereCollider>();
        obstacleMask = LayerMask.NameToLayer("Layer_Obstacle");
        wallMask = LayerMask.NameToLayer("Layer_Wall");

        // Initial direction of animal for TESTING
        transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        // Random number
        random01Coeficient = Random.Range(0.0f, 1.0f);
        randomSignCoeficient = Random.Range(0,2) * 2 - 1;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Update animal position
        rb.position += transform.forward * moveSpeed * Time.deltaTime;

        ObstacleSensing();
        WallSensing();
    }

    private void ObstacleSensing() 
    {
        // Sense obstacles
        obstacles = Physics.OverlapSphere(transform.position, obstAlertDist, 1 << obstacleMask);
        
        if (obstacles.Length > 0)
        {
            Debug.Log("There are " + obstacles.Length + " obstacles in alert range");

            foreach (Collider obstacle in obstacles)
            {
                // Detect direction and distance from obstacles to determine how the steer the animal
                Vector3 obstDirection = obstacle.transform.position - rb.position;
                Physics.Raycast(transform.position, obstDirection, out obstacleHit, obstAlertDist + 5);
                float obstDistance = obstacleHit.distance;

                //------------------ Debug 
                // Debug.Log("player position =  " + rb.position);
                // Debug.Log("obstacle position =  " + obstacle.transform.position);
                Debug.DrawLine(transform.position, obstacleHit.point);
                Debug.Log("Obst Distance =  " + obstacleHit.distance);
                //-------------------------------------------

                Steering(obstacleMask, obstDirection, obstDistance);
            }
        }    
    }

    private void WallSensing() 
    {
        // Sense walls
        walls = Physics.OverlapSphere(transform.position, wallAlertDist, 1 << wallMask);
        
        if (walls.Length > 0)
        {
            Debug.Log("There are " + walls.Length + " walls in alert range");

            foreach (Collider wall in walls)
            {
                // Detect direction and distance from wall to determine how the steer the animal
                Vector3 wallDirection = wall.transform.forward;
                Physics.Raycast(transform.position, wallDirection, out wallHit, wallAlertDist + 5);
                float wallDistance = wallHit.distance;

                //------------------ Debug 
                //Debug.Log("wall position =  " + wall.transform.position);
                Debug.DrawLine(transform.position, wallHit.point);
                Debug.Log("Wall Distance =  " + wallDistance);
                //-------------------------------------------

                Steering(wallMask, wallDirection, wallDistance);
            }
        }
     }

    private void Steering(int mask, Vector3 direction, float distance){

        float counterCoefficient = (mask == wallMask)? ( wallCounterCoefficient/distance ) : obstCounterCoefficient; 
        float avoidDistance = (mask == wallMask)? wallAvoidDist : obstAvoidDist;
        Vector3 forwardDirection = transform.forward; 

        // Culculate the max angular velocity the anaimal can do 
        maxAngularVel = breakForce / (mass * moveSpeed);
        maxRotateAngle = maxAngularVel * Time.fixedDeltaTime;
        
        // Counter force slow down the velocity perpendicular(toward) the obstacles/walls
        counterForce = counterCoefficient / Mathf.Pow(distance, 3.0f);
        counterAcceleration = counterForce / mass;

        // Find direction parallel to the obstacles/walls along the groung plane
        paraDirection = Quaternion.Euler(0, randomSignCoeficient * 90, 0) * direction;

        // Decrease perpendicular velocity
        forwardDirection -= counterAcceleration * Time.fixedDeltaTime * direction;

        //------------------ Debug 
         Debug.Log("Counter Force =  " + counterForce );
        //     Debug.Log("Counter Acceleration =  " + counterAcceleration);
        //     Debug.Log("Obstacle/Wall Distance Direction =  " + direction);
        //     Debug.Log("Forward Direction =  " + forwardDirection);
        //     Debug.Log("Parallel Direction =  " + paraDirection);
        //     Debug.Log("Obstacle/Wall Distance =  " + distance);    
        //-------------------------------------------
        
        if (distance < avoidDistance) {
            // Increase parallel velocity (along ground)
            forwardDirection += forwardDirection.magnitude * Mathf.Sin(maxRotateAngle) * paraDirection;  
        }
        else {
            // Randomize turning angle
            turnAngle = random01Coeficient * maxRotateAngle;

            // Increase parallel velocity (along ground)
            forwardDirection += forwardDirection.magnitude * Mathf.Sin(turnAngle) * paraDirection;
        }

        // Normalize velocity vector
        forwardDirection.Normalize();
        transform.rotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
     }
}
