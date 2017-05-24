using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour, fpsController {

    Rigidbody rb;
    ClientTest client;
    public Transform rayOrigin;
    LineRenderer line;
    public bool isDead;

	void Start () 
    {
        rb = GetComponent<Rigidbody>();
        GameObject temp = GameObject.FindGameObjectWithTag("Client");
        client = temp.GetComponent<ClientTest>();
        line = gameObject.GetComponent<LineRenderer>();
        line.enabled = false;
        Cursor.visible = false;
	}
	
	void Update () 
    {
        float xMov = Input.GetAxis("Vertical");
        float yMov = Input.GetAxis("Horizontal");
        float xLook = Input.GetAxis("Mouse X");
        float yLook = Input.GetAxis("Mouse Y");

        if (isDead == false)
        {
            if (xMov != 0)
            {
                Move(xMov, yMov);
                client.send("MOVE|" + xMov.ToString());
            }

            if (Input.GetKeyDown("Fire1"))
            {
                Shoot();
                client.send("SHOOT");
            }

            if (xLook != 0 || yLook != 0)
            {
                Turn(xLook, yLook);
                client.send("TURN|" + xLook.ToString() + "/" + yLook.ToString() + "|");
            }
        }
	}

    public void Move(float xMov, float zMov)
    {
        transform.Translate(new Vector3(xMov, 0, zMov));
    }

    public void Shoot()
    {
        StopCoroutine("fireLaser");
        StartCoroutine("fireLaser");
    }

    public void Turn(float xRot, float yRot)
    {
        this.transform.Rotate(xRot, yRot, 0);
    }

    IEnumerator fireLaser()
    {
        line.enabled = true;
        while (Input.GetButton("Fire1"))
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
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
