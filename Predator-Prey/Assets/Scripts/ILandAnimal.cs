using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILandAnimal
{
    // get position of the animal from the previous (physics) frame
    Vector3 GetPrevPosition();

    // get the animal's speed (vector magnitude)
    float GetSpeed();

    // get the calculated max deceleration magnitude
    float GetSpeedDown();

    // get the type of the animal (Predator or Prey)
    string GetTypeAnimal();

    // get the animal's velocity vector
    Vector3 GetVelocity();

    // calculate the max turning angle of the animal at the time
    // dependant upon mass, break force, and current speed
    float MaxTurn();

    // set the class name value of the animal (Predator or Prey)
    // can use Object.GetType().Name property
    void SetTypeAnimal();
}
