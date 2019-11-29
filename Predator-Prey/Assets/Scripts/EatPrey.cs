using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatPrey : MonoBehaviour
{
    Predator pred;
    PredMove pm;

    // distance in meters from a target for an automatic kill
    public float epsilon = 1.0f;

    // Awake is called before Start and just after prefabs are instantiated
    void Awake()
    {
        pred = GetComponentInParent<Predator>();
        pm = GetComponentInParent<PredMove>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // CHANGE TAG_DUMMY TO TAG_PREY
        if (pm.prey && other.tag == "tag_dummy" && other.attachedRigidbody == pm.prey)
        {
            Debug.Log("deactivation of prey");
            pm.prey.gameObject.SetActive(false);
        }
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
