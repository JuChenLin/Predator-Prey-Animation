using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1. Follow on the player's X/Z plane
/// 2. Smooth rotations around the player in 45 egree increments
/// </summary>
public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offsetPos;
    public float moveSpeed = 5;
    public float turnSpeed = 10;
    public float smoothSpeed = 0.5f;

    Quaternion targetRotation;
    Vector3 preTargetPos;
    Vector3 curTargetPos;
    Vector3 targetPos;
    bool smoothRotating = false;

    // Update is called once per frame
    void Update() {
        MoveWithTarget();
        LookAtTarget();

        if (Input.GetKeyDown(KeyCode.G) && !smoothRotating ){
            StartCoroutine("RotateAroundTarget", 45);
        }

        if (Input.GetKeyDown(KeyCode.H) && !smoothRotating ){
            StartCoroutine("RotateAroundTarget", -45);
        }
    }

    /// <summary>
    /// Move the camera to the player position + current camera offset
    /// Offset is modified by the RotateARoundTarget coroute
    /// </summary>
    void MoveWithTarget(){
        curTargetPos = target.position;
        targetPos = (preTargetPos + curTargetPos)/2 + offsetPos;
        preTargetPos = curTargetPos;
        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Use the Look vector (target - current) to aim the camera toward the player
    /// </summary>
    void LookAtTarget() {
        targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);    
    }

    /// <summary>
    /// This coroute can only have one instance running at a time 
    /// Determined by 'SmoothRotating'
    /// </summary>
    IEnumerator RotateAroundTarget(float angle) {
        Vector3 vel = Vector3.zero;
        Vector3 tragetOffsetPos = Quaternion.Euler(0, angle, 0) * offsetPos;
        float dist = Vector3.Distance(offsetPos, tragetOffsetPos);
        smoothRotating = true;

        while (dist > 0.01f) {
            offsetPos = Vector3.SmoothDamp(offsetPos, tragetOffsetPos, ref vel, smoothSpeed);
            dist = Vector3.Distance(offsetPos, tragetOffsetPos);
            yield return null;
        }

        smoothRotating = false;
        offsetPos = tragetOffsetPos;
    }
}
