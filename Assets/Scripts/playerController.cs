using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : physicsManager {

    Rigidbody rb;
    ClientTest client;

	void Start () 
    {
        rb = GetComponent<Rigidbody>();
        GameObject temp = GameObject.FindGameObjectWithTag("Client");
        client = temp.GetComponent<ClientTest>();
	}
	
	void Update () 
    {
        float accel = Input.GetAxis("Vertical");
        float pitch = Input.GetAxis("Mouse X");
        float yaw = Input.GetAxis("Mouse Y");
        float roll = Input.GetAxis("Horizontal");

        if (accel != 0)
        {
            adjustAcceleration(accel);
            client.send("ACCEL|" + accel.ToString());
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            stopAcceleration();
            client.send("STOPACCEL");
        }

        if (pitch != 0 || yaw != 0 || roll != 0)
        {
            turn(pitch, yaw, roll);
            client.send("ROTATE|" + pitch.ToString() + "/" + yaw.ToString() + "/" + roll.ToString());
        }

        rb.AddForce(Vector3.forward * accelforce);
	}

    public override void adjustAcceleration(float accelValue)
    {
        accelforce += accelValue;
        if (accelforce < 0)
        {
            accelforce = 0;
        }

        if (accelforce >= maxAccelForce)
        {
            accelforce = maxAccelForce;
        }
    }

    public override void stopAcceleration()
    {
        accelforce = 0;
    }

    public override void turn(float xRot, float yRot, float zRot)
    {
        this.transform.Rotate(xRot, yRot, zRot);
    }
}
