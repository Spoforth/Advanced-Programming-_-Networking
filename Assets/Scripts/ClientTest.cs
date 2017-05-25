using System.Collections;
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
    playerController player;
    string ipAddress;
    int health = 100;
    byte error;

    public delegate void Respawn(float time);
    public event Respawn RespawnMe;
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
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelId = config.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology, 8000);
        Debug.Log("Socket Open. Host ID is " + hostId);
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
                                continue;
                            }
                            else
                            {
                                GameObject newPlayer = Instantiate(otherPlayers, new Vector3(playerX, playerY, playerZ), transform.rotation);
                                playerList.Add(playerID, newPlayer);
                            }
                        }
                        break;
                    case "UPDATE":
                        for(int i = 1; i <= splitData.Length; i++)
                        {
                            int playerID = int.Parse(splitData[1]);
                            if (playerID == connectionId)
                            {
                                continue;
                            }
                            GameObject obj = playerList[playerID];
                            Vector3 oldPos = playerList[playerID].transform.position;
                            Vector3 oldRot = playerList[playerID].transform.eulerAngles;
                            Vector3 pos =  new Vector3(float.Parse(splitData[3]), float.Parse(splitData[3]), float.Parse(splitData[4]));
                            obj.transform.position = Vector3.Lerp(oldPos, pos, 0.2f);
                            obj.transform.rotation = Quaternion.FromToRotation(oldRot, new Vector3(0, float.Parse(splitData[4]), 0));
                            obj.transform.GetChild(0).transform.rotation = Quaternion.FromToRotation(oldRot, new Vector3(float.Parse(splitData[5]), 0, 0));
                        }
                        break;
                    case "INPUTPROCESSED":
                        Vector3 newPos = Vector3.zero;
                        Vector3 newRot = Vector3.zero;

                        for (int i = 0; i < inputList.Count; i++)
                        {
                            string[] inputArray = inputList[i].Split('|');
                            if (int.Parse(splitData[1]) < int.Parse(inputArray[0]))
                            {
                                continue;
                            }
                            else if (int.Parse(splitData[1]) == int.Parse(inputArray[0]))
                            {
                                string[] posArray = splitData[2].Split('/');
                                string[] rotArray = splitData[3].Split('/');
                                newPos = new Vector3(float.Parse(posArray[0]), float.Parse(posArray[1]), float.Parse(posArray[2]));
                                newRot = new Vector3(float.Parse(rotArray[0]), float.Parse(rotArray[1]), 0);
                            }
                            else
                            {
                                switch (inputList[1])
                                {
                                    case "MOVE":
                                        newPos += new Vector3(float.Parse(inputList[2]), float.Parse(inputList[3]), float.Parse(inputList[4]));
                                        break;
                                    case "TURN":
                                        newRot += new Vector3(float.Parse(inputList[2]), float.Parse(inputList[3]), 0);
                                        break;
                                }
                            }
                        }
                        transform.position = Vector3.Lerp(transform.position, newPos, 0);
                        transform.rotation = Quaternion.FromToRotation(transform.eulerAngles, new Vector3(0, newRot.y));
                        transform.GetChild(0).transform.rotation = Quaternion.FromToRotation(transform.eulerAngles, new Vector3(newRot.x, 0));
                        break;
                    case "DC":
                        Destroy(playerList[int.Parse(splitData[1])]);
                        playerList.Remove(int.Parse(splitData[1]));
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
        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", 0, 0, out error);
        if ((NetworkError)error == NetworkError.Ok)
        {
            Debug.Log("Connected to Server. Connection ID: " + connectionId);
            Instantiate(playerPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError("NETWORK ERROR CODE:" + error.ToString());
        }
        UICanvas.enabled = false;
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
        //Debug.Log("Sent Message: " + message);
    }

    public void GetShot(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            if (RespawnMe != null)
            {
                RespawnMe(5f);
                player.isDead = true;
            }
        }
    }
}
