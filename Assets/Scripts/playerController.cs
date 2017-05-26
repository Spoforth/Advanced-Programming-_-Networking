using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : player {

    ClientTest client;
    public GameObject rayOrigin;
    public GameObject head;
    LineRenderer line;
    public float xLimit;

	void Start () 
    {
        GameObject temp = GameObject.FindGameObjectWithTag("Client");
        client = temp.GetComponent<ClientTest>();
        Cursor.visible = false;
	}
	
	void Update () 
    {
        float xMov = Input.GetAxis("Horizontal");
        float zMov = Input.GetAxis("Vertical");
        float yLook = Input.GetAxis("Mouse Y");
        float xLook = Input.GetAxis("Mouse X");

        if (isDead == false)
        {
            if (xMov != 0 || zMov != 0)
            {
                Move(xMov, zMov);
                client.send("MOVE|" + xMov.ToString() + "|" + zMov.ToString());
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Shoot();
                client.send("SHOOT");
            }

            if (yLook != 0 || xLook != 0)
            {
                Turn(yLook, xLook);
                client.send("TURN|" + yLook.ToString() + "/" + xLook.ToString() + "|");
            }
        }
	}

    public override void Turn(float xRot, float yRot)
    {
        transform.Rotate(0, yRot, 0);
        head.transform.Rotate(-xRot, 0, 0);
        head.transform.localRotation = Quaternion.Euler(head.transform.eulerAngles.x, 0, 0);
    }

    IEnumerator fireLaser() //visual only, actual damage handled by server
    {
        line = rayOrigin.GetComponent<LineRenderer>();
        line.enabled = true;
        while (Input.GetButton("Fire1"))
        {
            Ray ray = new Ray(rayOrigin.transform.position, rayOrigin.transform.forward);
            RaycastHit hit;
            line.SetPosition(0, ray.origin);
            if (Physics.Raycast (ray, out hit, 100))
            {
                line.SetPosition(1, hit.point);
            }
            else
            {
                line.SetPosition(1, ray.GetPoint(100));
            }
            yield return null;
        }
        line.enabled = false;
    }

}
