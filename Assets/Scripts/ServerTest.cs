﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerTest : MonoBehaviour {

    public GameObject playerObject;

    int connectionId;
    int maxConnections = 10;
    int reliableChannelId;
    int hostId;
    int socketPort = 8888;
    bool isStarted = false;
    private IEnumerator coroutine;
    public Dictionary<int, GameObject> clients = new Dictionary<int, GameObject>();

    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelId = config.AddChannel(QosType.Reliable); ;
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology, socketPort, null); // null means anyone can join, used for servers
        Debug.Log("Socket Open. Host ID is " + hostId);
        isStarted = true;

        //coroutine = ExecuteAfterTime(0.2f);
        //StartCoroutine(coroutine);
    }

    void Update()
    {
        int recHostId;
        int recConnectionId;
        int recChannelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostId, out recConnectionId, out recChannelId, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection event recieved. recHostID: "  + recHostId + " recConnedID: " + recConnectionId);
                GameObject clone = Instantiate(playerObject, transform.position, transform.rotation);
                clients.Add(recConnectionId, clone);
                ServerClient cloneSC = clone.GetComponent<ServerClient>();
                cloneSC.HostID = recHostId;
                string message = "PLAYERS|";
                foreach (KeyValuePair<int, GameObject> c in clients)
                {
                    message += c.Key.ToString() + "/" + c.Value.transform.position.x.ToString() + "/" + c.Value.transform.position.y.ToString() + "/" + c.Value.transform.position.z.ToString() + "|";
                }
                Send(message, reliableChannelId, connectionId);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving: " + msg);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "MOVE":
                        Move(clients[recConnectionId], splitData[1], splitData[2]);
                        break;
                }

                break;
            case NetworkEventType.DisconnectEvent:
                Destroy(clients[recConnectionId]);
                clients.Remove(recConnectionId);
                Send("DC|" + recConnectionId, reliableChannelId, connectionId);
                break;
        }
    }

    void FixedUpdate()
    {
        string msg = "POSUPDATE|";
        foreach (KeyValuePair<int, GameObject> entry in clients)
        {
            msg += entry.Key + "/" + entry.Value.transform.position.x + "/" + entry.Value.transform.position.y + "/" + entry.Value.transform.position.z + "|";
        }
        Debug.Log("Updating Positions. Message is: " + msg);
        Send(msg, reliableChannelId, connectionId);
    }

    private void Move(GameObject obj, string x, string y)
    {
        float xMov = float.Parse(x);
        float yMov = float.Parse(y);
        obj.transform.Translate(xMov, 0, yMov);
    }

    public void Send(string message, int channelID, int cnID)
    {
        byte error;
        Debug.Log("Sending: " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (KeyValuePair<int, GameObject> entry in clients)
        {
            NetworkTransport.Send(hostId, entry.Key, channelID, msg, message.Length * sizeof(char), out error);
        }
    }

    public void sendToHost(string message, int hostID, int ChannelID, int cnID)
    {
        byte error;
        Debug.Log("Sending: " + message + "to player cnID " + cnID);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, cnID, ChannelID, msg, message.Length * sizeof(char), out error);
    }

    private void SendToPlayer(string message, int hostID, int channelID, int cnID)
    {
        byte error;
        Debug.Log("Sending to host" + hostId + " : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, cnID, channelID, msg, msg.Length * sizeof(char), out error);
    }

    IEnumerator ExecuteAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        string msg = "POSUPDATE|";
        foreach (KeyValuePair<int, GameObject> entry in clients)
        {
            msg += entry.Value.transform.position.x + "/" + entry.Value.transform.position.y + "/" + entry.Value.transform.position.z + "|";
        }
        Debug.Log("Updating Positions. Message is: " + msg);
        Send(msg, reliableChannelId, connectionId);
    }
}
