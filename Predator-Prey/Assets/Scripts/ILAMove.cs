using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILAMove
{
    // accelerate to the desired speed you wish to reach; will be limited by a maximum speed
    void Accelerate(float desSpeed);

    // provide the maximum speed you are allowed, as well as the desired angle
    // the method DegAngleToTarget(Vector3 target) can provide such information
    void AdjSpeedForAngle(float maxSpeed, float desAngle);

    // check if another Rigidbody is in view
    // the isHunting parameter is a "full alert" state, meaning both binocular FOV
    // and monocular FOV can be implemented
    bool CheckFOV(Rigidbody other, bool isHunting);



    // decelerate to the desired speed you wish to reach; will be limited by 0 speed
    void Decelerate(float desSpeed);

    // angle in degrees to your target, based upon your facing direction from the provided position
    // could use eyes of the animal as the source/view point (target would be the destination point) 
    float DegAngleToTarget(Vector3 target, Vector3 startPos);

    // the vector direction to your target from the provided position; could use eyes of the animal as the
    // source/view point (target would be the destination point)
    Vector3 DirToTarget(Vector3 target, Vector3 startPos);

    // implement behavior to seek a target position
    // provide the maximum speed you are allowed, as well as the desired target position
    // target can also be the Rigidbody or Transform position of a GameObject
    void Seek(float maxSpeed, Vector3 target);
}
