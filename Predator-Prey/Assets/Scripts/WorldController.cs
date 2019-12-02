using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    // used to assign which predator object to CameraController
    public GameObject mainCamera;
    private CameraController cc;

    // used for respawning Predator
    public GameObject predator;

    public GameObject predPrefab;
    public GameObject preyPrefab;

    // key codes for predator respawn
    // for respawn in place
    public KeyCode respawnIP = KeyCode.R;
    // for random respawn
    public KeyCode respawnRand = KeyCode.C;
    // for disabling/reenabling follow camera
    public KeyCode follow = KeyCode.F;

    // minimum allowed distance from another animal
    public float allowedDist = 5.0f;

    // # of predators and prey you wish to spawn
    readonly private int numPredators = 0;
    readonly private int numPrey = 6;

    readonly private float xLeftLimit = 35.0f;
    readonly private float xRightLimit = 250.0f;
    readonly private float zFrontLimit = 25.0f;
    readonly private float zBackLimit = 200.0f;
    readonly private float safeRayHeight = 60.0f;
    readonly private float rayLength = 70.0f;

    private Vector3 spawnPoint;

    private int predMask;
    private int preyMask;

    void Awake()
    {
        cc = mainCamera.GetComponent<CameraController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // spawn the Predator(s) first
        for (int i = 0; i < numPredators; i++)
        {
            ChooseTarget();
            Spawn(predPrefab);
        }

        // then spawn the Prey
        for (int i = 0; i < numPrey; i++)
        {
            ChooseTarget();
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
            RespawnPredRandom();
        }
        if (Input.GetKeyDown(follow))
        {
            // toggle on and off
            cc.enabled = !cc.enabled;

            if (!cc.enabled)
            {
                Debug.Log("Camera follow toggled");
                mainCamera.transform.position = new Vector3(125.0f, 60.0f, 125.0f);
                mainCamera.transform.rotation = Quaternion.Euler(new Vector3(45.0f, -45.0f, 0.0f));
            }
        }
    }

    void ChooseTarget()
    {
        bool foundTarget = false;
        float xCoord = Random.Range(xLeftLimit, xRightLimit);
        float zCoord = Random.Range(zFrontLimit, zBackLimit);

        RaycastHit hit;

        while (!foundTarget)
        {
            if (Physics.Raycast(new Vector3(xCoord, safeRayHeight, zCoord), Vector3.down, out hit, rayLength))
            {
                if (hit.collider.name == "Terrain_0_0_e6328e0c-e78e-4e73-8b36-fcdb7200ddb6")
                {
                    spawnPoint = hit.point;

                    if(FullScan())
                        foundTarget = true;
                }
            }
        }
    }

    public bool FullScan()
    {
        Collider[] inRange = Physics.OverlapSphere(spawnPoint, allowedDist, (1 << preyMask) | (1 << predMask));

        if (inRange.Length == 0)
            return true;

        foreach (Collider c in inRange)
        {
            if (c.attachedRigidbody)
            {
                Debug.Log("WC: Checking allowed distance!");
                // ignore trigger colliders
                if (!c.isTrigger && (c.attachedRigidbody.position - spawnPoint).magnitude < allowedDist)
                    return false;
            }
        }

        return true;
    }

    public void RespawnPredInPlace()
    {
        spawnPoint = predator.transform.position;
        cc.enabled = false;
        Destroy(predator);
        Spawn(predPrefab);
    }

    public void RespawnPredRandom()
    {
        Debug.Log("!!!!Predator spawned!!!!");
        ChooseTarget();
        cc.enabled = false;
        Destroy(predator);
        Spawn(predPrefab);
    }

    public void Spawn(GameObject pf)
    {
        Debug.Log("!!!!!SPAWNING!!!!!");
        // generate random y-axis rotation
        float rot = Random.Range(-180.0f, 180.0f);

        if (pf.tag == "tag_Predator")
        {
            predator = Instantiate(pf, spawnPoint, Quaternion.Euler(new Vector3(0.0f, rot, 0.0f)));
            cc.enabled = true;
            cc.target = predator.transform;
        }
        else
            Instantiate(pf, spawnPoint, Quaternion.Euler(new Vector3(0.0f, rot, 0.0f)));
    }
}
