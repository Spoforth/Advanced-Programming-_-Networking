using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class physicsManager : MonoBehaviour {

    public float accelforce { get; set; }
    public float maxAccelForce {get; set;}
    public float rotSpeed { get; set; }
    public float damage { get; set; }
    public float health { get; set; }

    public abstract void adjustAcceleration(float accel);
    public abstract void stopAcceleration();
    public abstract void turn(float xRot, float yRot, float zRot);
}
