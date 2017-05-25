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
    public float respawnTime = 3f;
    public Dictionary<int, GameObject> clients = new Dictionary<int, GameObject>();
    public Transform[] spawnPoints;
    
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelId = config.AddChannel(QosType.Reliable); ;
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology, socketPort, null); // null means anyone can join, used for servers
        Debug.Log("Socket Open. Host ID is " + hostId);
        isStarted = true;

        coroutine = ExecuteAfterTime(0.2f);
        StartCoroutine(coroutine);
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
                cloneSC.ConnectionID = recConnectionId;
                string message = "PLAYERS|";
                foreach (KeyValuePair<int, GameObject> c in clients)
                {
                    message += c.Key.ToString() + "/" + c.Value.transform.position.x.ToString() + "/" + c.Value.transform.position.y.ToString() + "/" + c.Value.transform.position.z.ToString() + "|";
                }
                Send(message, reliableChannelId);
                break;
            case NetworkEventType.DataEvent:
                GameObject playerOBJ = clients[recConnectionId];
                ServerClient player = playerOBJ.GetComponent<ServerClient>();
                player.addToQueue(recBuffer);
                break;
            case NetworkEventType.DisconnectEvent:
                Destroy(clients[recConnectionId]);
                clients.Remove(recConnectionId);
                Send("DC|" + recConnectionId, reliableChannelId);
                break;
        }
    }

    void FixedUpdate()
    {

    }

    public void Send(string message, int channelID)
    {
        byte error;
        Debug.Log("Sending: " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (KeyValuePair<int, GameObject> entry in clients)
        {
            NetworkTransport.Send(hostId, entry.Key, channelID, msg, message.Length * sizeof(char), out error);
        }
    }

    public void SendToPlayer(string message, int channelID, int cnID)
    {
        byte error;
        Debug.Log("Sending to host" + hostId + " : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, cnID, channelID, msg, msg.Length * sizeof(char), out error);
    }

    public void StartSpawnProcess(int playerID)
    {
        StartCoroutine("SpawnPlayer", playerID);
    }

    IEnumerator ExecuteAfterTime(float time)
    {
        string msg = "UPDATE";
        foreach (KeyValuePair<int, GameObject> entry in clients)
        {
            GameObject obj = entry.Value;
            ServerClient sc = obj.GetComponent<ServerClient>();
            sc.runQueue();
            msg += entry.Key + "|" + entry.Value.transform.position.x + "/" + entry.Value.transform.position.y + "/" + entry.Value.transform.position.z + "|" + entry.Value.transform.rotation.x + "/" + entry.Value.transform.rotation.y + "|" + sc.health + "|" + sc.isDead.ToString() + "|" + sc.firing.ToString() + "|";
        }
        Debug.Log("Updating Positions. Message is: " + msg);
        Send(msg, reliableChannelId);
        yield return new WaitForSeconds(time);
    }

    IEnumerator SpawnPlayer(int playerID)
    {
        yield return new WaitForSeconds(respawnTime);
        int index = Random.Range(0, spawnPoints.Length);
        GameObject player = clients[playerID];
        player.transform.position = spawnPoints[index].position;
        ServerClient sc = player.GetComponent<ServerClient>();
        sc.isDead = false;
        sc.health = sc.maxHealth;
        player.transform.GetChild(0).gameObject.SetActive(true);
    }
}
