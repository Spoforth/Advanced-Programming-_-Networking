using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerClient : physicsManager {

    public int ConnectionID;
    public string playerName;
    public ServerTest server;

    public Rigidbody rb;
    Queue<string> inputQueue = new Queue<string>();

    void Start()
    {
        GameObject serverOBJ = GameObject.FindGameObjectWithTag("Server");
        server = serverOBJ.GetComponent<ServerTest>();
    }

    void Update()
    {
        
    }

    public override void adjustAcceleration(float accel)
    {
        accelforce += accel;
    }

    public override void stopAcceleration()
    {
        accelforce = 0;
    }

    public override void turn(float xRot, float yRot, float zRot)
    {
        transform.Rotate(xRot, yRot, zRot);
    }

    public void AddToQueue(string message)
    {
        inputQueue.Enqueue(message);
    }

    public void RunQueue()
    {
        foreach (string i in inputQueue)
        {
            string[] splitData = i.Split('|');

            switch (splitData[1])
            {
                case "ACCEL":
                    adjustAcceleration(float.Parse(splitData[2]));
                    server.Send("INPUT PROCESSED|" + splitData[0] + transform.position.x + transform.position.y + transform.position.z, server.reliableChannelId, ConnectionID);
                    break;
                case "STOPACCEL":
                    stopAcceleration();
                    server.Send("INPUT PROCESSED|" + splitData[0] + transform.position.x + transform.position.y + transform.position.z, server.reliableChannelId, ConnectionID);
                    break;
                case "ROTATE":
                    turn(float.Parse(splitData[2]), float.Parse(splitData[3]), float.Parse(splitData[4]));
                    server.Send("INPUT PROCESSED|" + splitData[0] + transform.position.x + transform.position.y + transform.position.z, server.reliableChannelId, ConnectionID);
                    break;
            }

        }
        inputQueue.Clear();
    }
}
