using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : PhysicsManager {

    Rigidbody rb;

	void Start ()
    {
        rb = GetComponent<Rigidbody>();
	}
	
	void FixedUpdate ()
    {
        rb.AddForce(transform.forward * accelForce);

        float _xRot = Input.GetAxisRaw("MouseX");
        float _yRot = Input.GetAxisRaw("MouseY");
        float _zRot = Input.GetAxisRaw("Horizontal");
        float _force = Input.GetAxisRaw("Vertical");



        adjustAcceleration(_force);
        turn(_xRot, _yRot);
        roll(_zRot);  
	}

    public override void adjustAcceleration(float force)
    {

        accelForce += force;

        if (accelForce > maxAccelForce)
        {
            accelForce = maxAccelForce;
        }

        if (accelForce < 0)
        {
            accelForce = 0;
        }
    }

    public override void turn(float xRot, float yRot)
    {
        transform.Rotate(xRot, yRot, 0);
    }

    public override void roll(float zRot)
    {
        transform.Rotate(0, 0, zRot);
    }
}
