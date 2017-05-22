using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerClient : MonoBehaviour, fpsController{

    public int HostID;
    public int ConnectionID;
    public string playerName;
    public ServerTest server;
    int maxConnections = 10;
    int reliableChannelId;
    int hostId;
    public Transform rayOrigin;
    public LineRenderer line;

    public Rigidbody rb;
    List<string> inputList = new List<string>();
    int inputCount = 0; //how many inputs have been sent
    int inputLimit; //how many inputs can be saved

    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelId = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology, 0);
        Debug.Log("Socket Open. Host ID is " + hostId);
        GameObject serverOBJ = GameObject.FindGameObjectWithTag("Server");
        server = serverOBJ.GetComponent<ServerTest>();
    }

    void Update()
    {
        int recConnectionId;
        int recChannelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recNetworkEvent = NetworkTransport.ReceiveFromHost(HostID, out recConnectionId, out recChannelId, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving: " + msg);
                string[] splitData = msg.Split('|');

                switch (splitData[1])
                {
                    case "ACCEL":
                        Move(float.Parse(splitData[2]));
                        break;
                    case "STOPACCEL":
                        Shoot();
                        break;
                    case "ROTATE":
                        string[] turnData = splitData[2].Split('/');
                        Turn(float.Parse(turnData[0]), float.Parse(turnData[1]), float.Parse(turnData[2]));
                        break;
                }

                break;
        }

        if (inputList.Count > inputLimit)
        {
            inputList.RemoveAt(0);
        }
    }

    public void Move(float xMov, float zMov)
    {
        
    }

    public void Shoot()
    {
        StopCoroutine("fireLaser");
        StartCoroutine("fireLaser");
    }

    public void Turn(float xRot, float yRot, float zRot)
    {
        transform.Rotate(xRot, yRot, zRot);
    }

    IEnumerator fireLaser()
    {
        line.enabled = true;
        while (Input.GetButton("Fire1"))
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            RaycastHit hit;
            line.SetPosition(0, ray.origin);
            if (Physics.Raycast(ray, out hit, 100))
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
