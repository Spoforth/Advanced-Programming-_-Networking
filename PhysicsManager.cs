using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsManager : MonoBehaviour {

    public float maxAccelForce;
    public float accelForce;
    public float rotSpeed;
    public float damage;
    public float health;

    public abstract void adjustAcceleration(float force);
    public abstract void turn(float xRot, float yRot);
    public abstract void roll(float zRot);
	
}
