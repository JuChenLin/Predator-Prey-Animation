using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    // used to assign which predator object to CameraController
    public GameObject mainCamera;
    private CameraController cc;
    private FlyCamera fly;

    // used for respawning Predator
    public GameObject predator;
    Rigidbody rigid;

    public GameObject predPrefab;
    public GameObject preyPrefab;

    // key codes for predator respawn
    // for respawn in place

    public KeyCode respawnIP = KeyCode.R;
    // for respawn to "safe" start point
    public KeyCode respawnRand = KeyCode.C;
    // for disabling/reenabling follow camera
    public KeyCode follow = KeyCode.F;

    // minimum allowed distance from another animal
    public float allowedDist = 3.0f;

    // # of prey you wish to spawn
    readonly private int numPrey = 12;
    private int preySpawned = 0;

    // max number of tries to attempt choosing target
    // before a default is chosen (avoid infinite loop)
    readonly int maxTries = 5;
    private int tryCount = 1;

    readonly private float xLeftLimit = 45.0f;
    readonly private float xRightLimit = 240.0f;
    readonly private float zFrontLimit = 35.0f;
    readonly private float zBackLimit = 190.0f;
    readonly private float safeRayHeight = 60.0f;
    readonly private float rayLength = 100.0f;

    private Vector3 spawnPoint;
    private Vector3 startPoint = new Vector3(68.0f, 0.7f, 180.0f);

    private int predMask;
    private int preyMask;
    private int obstacleMask;

    void Awake()
    {
        cc = mainCamera.GetComponent<CameraController>();
        fly = mainCamera.GetComponent<FlyCamera>();

        obstacleMask = LayerMask.NameToLayer("layer_Obstacle");
        preyMask = LayerMask.NameToLayer("layer_Prey");
        predMask = LayerMask.NameToLayer("layer_Predator");
    }

    // Start is called before the first frame update
    void Start()
    {
        rigid = predator.GetComponent<Rigidbody>();

        cc.enabled = true;
        fly.enabled = false;

        // spawn the Prey
        for (int i = 0; i < numPrey; i++)
        {
            if (!ChooseTarget())
            {
                Debug.Log("could not spawn this Prey!");
            }
            else
                Spawn(preyPrefab);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(respawnIP))
        {
            RespawnPredInPlace();
        }

        if (Input.GetKeyDown(respawnRand))
        {
            RespawnPredStart();
        }
        if (Input.GetKeyDown(follow))
        {
            // switch cameras
            cc.enabled = !cc.enabled;

            if (cc.enabled)
            {
                fly.enabled = false;
            }
            else
                fly.enabled = true;


            if (fly.enabled)
            {
                mainCamera.transform.position = new Vector3(125.0f, 60.0f, 125.0f);
                mainCamera.transform.rotation = Quaternion.Euler(new Vector3(45.0f, -45.0f, 0.0f));
            }
        }
    }

    bool ChooseTarget()
    {
        bool foundTarget = false;
        float xCoord = Random.Range(xLeftLimit, xRightLimit);
        float zCoord = Random.Range(zFrontLimit, zBackLimit);

        Ray ray = new Ray(new Vector3(xCoord, safeRayHeight, zCoord), Vector3.down);
        RaycastHit hit;
        TerrainCollider tc = Terrain.activeTerrain.GetComponent<TerrainCollider>();

        while (tryCount <= maxTries && !foundTarget)
        {
            if (tc.Raycast(ray, out hit, rayLength))
            {
                spawnPoint = hit.point;

                if (FullScan())
                {
                    Debug.Log("WC: Found a target!");
                    foundTarget = true;
                }
                else
                {
                    Debug.Log("WC: ChooseTarget try # " + tryCount + " at " + hit.point);
                    tryCount++;
                }
            }
        }

        return foundTarget;
    }

    public bool FullScan()
    {
        Collider[] inRange = Physics.OverlapSphere(spawnPoint, allowedDist, (1 << preyMask) | (1 << predMask) | (1 << obstacleMask));

        if (inRange.Length == 0)
            return true;

        foreach (Collider c in inRange)
        {
            if (c.attachedRigidbody)
            {
                Debug.Log("WC: Rigidbody detected belongs to " + c.name);
                // ignore trigger colliders
                if (!c.isTrigger && (c.attachedRigidbody.position - spawnPoint).magnitude < allowedDist)
                    return false;
            }
            else
            {
                Debug.Log("WC: obstacle detected belongs to " + c.name);
                if (!c.isTrigger && (c.transform.position - spawnPoint).magnitude < allowedDist)
                    return false;
            }
        }

        Debug.Log("WC: Found a spot!");
        return true;
    }

    public void RespawnPredInPlace()
    {
        spawnPoint = predator.transform.position;
        SpawnPred();
    }

    public void RespawnPredStart()
    {
        Debug.Log("!!!!Predator spawned!!!!");
        spawnPoint = startPoint;
        SpawnPred();
    }

    public void Spawn(GameObject pf)
    {
        Debug.Log("!!!!!SPAWNING " + pf.name + "!!!!!");
        // generate random y-axis rotation
        float rot = Random.Range(-180.0f, 180.0f);

        Instantiate(pf, spawnPoint, Quaternion.Euler(new Vector3(0.0f, rot, 0.0f)));
    }

    public void SpawnPred()
    {
        Debug.Log("!!!!!SPAWNING Predator!!!!!");
        // generate random y-axis rotation
        float rot = Random.Range(-180.0f, 180.0f);

        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        rigid.position = spawnPoint;
        rigid.rotation = Quaternion.Euler(new Vector3(0.0f, rot, 0.0f));
    }
}
