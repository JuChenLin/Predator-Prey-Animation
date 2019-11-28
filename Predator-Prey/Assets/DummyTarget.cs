using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyTarget : MonoBehaviour
{
    public float currSpeed = 0.0f;
    private Vector3 prevPosition;
    public float accel = 10.0f;
    public float maxSpeed = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        prevPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        currSpeed = ((transform.position - prevPosition).magnitude) / Time.deltaTime;
        prevPosition = transform.position;
        float maxAccel = ((currSpeed + accel) < maxSpeed) ? accel : (maxSpeed - currSpeed);
        transform.position += transform.forward * (currSpeed + maxAccel) * Time.deltaTime;
    }
}
