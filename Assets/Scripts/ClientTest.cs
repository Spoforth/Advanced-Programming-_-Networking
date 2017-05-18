﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ClientTest : MonoBehaviour {

    int connectionId;
    int maxConnections = 10;
    int reliableChannelId;
    int hostId;
    int socketPort = 8888;
    bool isStarted = false;
    byte error;

    public GameObject playerPrefab;
    public GameObject otherPlayers;
    public int inputCount; //how many inputs have been sent
    public int inputLimit; //how many inputs can be saved
    public Dictionary<int, GameObject> playerList = new Dictionary<int, GameObject>();
    public List<string> inputList = new List<string>();
    public Canvas UICanvas;

	void Start () 
    {
        NetworkTransport.Init();
	}
	
	void Update () 
    {
        int recHostId;
        int recConnectionId;
        int recChannelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostId, out recConnectionId, out recChannelId, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.ConnectEvent:
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving: " + msg);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "PLAYER":
                        for (int i = 1; i <= splitData.Length; i++)
                        {
                            string[] message = splitData[i].Split('/');
                            int playerID = int.Parse(message[0]);
                            float playerX = float.Parse(message[1]);
                            float playerY = float.Parse(message[2]);
                            float playerZ = float.Parse(message[3]);
                            if (playerList.ContainsKey(int.Parse(message[0])))
                            {
                                return;
                            }
                            else
                            {
                                GameObject newPlayer = Instantiate(otherPlayers, new Vector3(playerX, playerY, playerZ), transform.rotation);
                                playerList.Add(playerID, newPlayer);
                            }
                        }
                        break;
                    case "POSUPDATE":
                        for(int i = 1; i <= splitData.Length; i++)
                        {

                            string[] temp = splitData[i].Split('/');
                            Vector3 pos = Vector3.zero;
                            pos.x = float.Parse(temp[1]);
                            pos.y = float.Parse(temp[2]);
                            pos.z = float.Parse(temp[3]);
                            Vector3 oldPos = playerList[int.Parse(temp[0])].transform.position;

                            if (oldPos != pos)
                            {
                                playerList[int.Parse(temp[0])].transform.position = pos;
                            }
                        }
                        break;
                }

                break;
        }

        if (inputList.Count > inputLimit)
        {
            inputList.RemoveAt(0);
        }
	}

    public void Connect()
    {
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelId = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology,0);
        Debug.Log("Socket Open. Host ID is " + hostId);
        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", socketPort, 0, out error);
        Debug.Log("Connected to Server. Connection ID: " + connectionId);
        UICanvas.enabled = false;
        isStarted = true;
        Instantiate(playerPrefab, transform.position, transform.rotation);

    }

    public void Disconnect()
    {
        byte error;
        NetworkTransport.Disconnect(hostId, connectionId, out error);
        Debug.Log("Disconnected from server");
    }

    public void send(string message)
    {
        message = inputCount + "|" + message;
        byte error;
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, connectionId, reliableChannelId, buffer, message.Length * sizeof(char), out error);
        inputCount++;
        inputList.Add(message);
        Debug.Log("Sent Message: " + message);
    }
}
