using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
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

    public GameObject playerPrefab;
    public GameObject otherPlayers; //represent other players
    public int inputCount; //how many inputs have been sent
    public int inputLimit; //how many inputs can be saved
    public Dictionary<int, GameObject> playerList = new Dictionary<int, GameObject>();
    public List<string> inputList = new List<string>();
    public Canvas UICanvas;

	void Start () 
    {
        //NETWORK SETUP
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
            case NetworkEventType.DataEvent:
                //conver package from bytes to string, split data into an array, then choose what to do with that data
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving: " + msg);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "PLAYER":
                        //spawn all players at their positions, sent whenever server gets new connection
                        for (int i = 1; i <= splitData.Length; i++)
                        {
                            string[] message = splitData[i].Split('/');
                            int playerID = int.Parse(message[0]);
                            float playerX = float.Parse(message[1]);
                            float playerY = float.Parse(message[2]);
                            float playerZ = float.Parse(message[3]);
                            if (playerList.ContainsKey(int.Parse(message[0]))) //if player has already been spawned, skip this iteration
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
                        //updates the positions of each player that isn't the client
                        for(int i = 1; i <= splitData.Length; i++)
                        {
                            int playerID = int.Parse(splitData[1]);
                            if (playerID == connectionId) //if this position is the clients, skip the iteration
                            {
                                continue;
                            }
                            GameObject obj = playerList[playerID];
                            Vector3 oldPos = playerList[playerID].transform.position;
                            Vector3 oldRot = playerList[playerID].transform.eulerAngles;
                            Vector3 pos =  new Vector3(float.Parse(splitData[3]), float.Parse(splitData[3]), float.Parse(splitData[4])); //get x y z position from data sent
                            obj.transform.position = Vector3.Lerp(oldPos, pos, 0.1f); //lerp to new position 
                            obj.transform.rotation = Quaternion.FromToRotation(oldRot, new Vector3(0, float.Parse(splitData[4]), 0)); //rotate body
                            obj.transform.GetChild(0).transform.rotation = Quaternion.FromToRotation(oldRot, new Vector3(float.Parse(splitData[5]), 0, 0)); //rotate head
                        }
                        break;
                    case "INPUTPROCESSED":
                        //checks which inputs have been processed by the server and updates the player's position acoording to that
                        Vector3 newPos = Vector3.zero;
                        Vector3 newRot = Vector3.zero;
                        for (int i = 0; i < inputList.Count; i++)
                        {
                            string[] inputArray = inputList[i].Split('|');
                            if (int.Parse(splitData[1]) < int.Parse(inputArray[0])) //these inputs were processed by the server, but are not the most recent
                            {
                                continue;
                            }
                            else if (int.Parse(splitData[1]) == int.Parse(inputArray[0])) //most recent input processed, gets new position data 
                            {
                                string[] posArray = splitData[2].Split('/');
                                string[] rotArray = splitData[3].Split('/');
                                newPos = new Vector3(float.Parse(posArray[0]), float.Parse(posArray[1]), float.Parse(posArray[2]));
                                newRot = new Vector3(float.Parse(rotArray[0]), float.Parse(rotArray[1]), 0);
                            }
                            else
                            {
                                switch (inputList[1]) //reapplies inputs that haven't been processed by the server
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
                        //applies new position & rotation data
                        transform.position = Vector3.Lerp(transform.position, newPos, 0); 
                        transform.rotation = Quaternion.FromToRotation(transform.eulerAngles, new Vector3(0, newRot.y));
                        transform.GetChild(0).transform.rotation = Quaternion.FromToRotation(transform.eulerAngles, new Vector3(newRot.x, 0));
                        //get all inputs that have been processed and removes them
                        IEnumerable<string> query = inputList.Where(x => int.Parse(x[0].ToString()) <= int.Parse(splitData[1])).OrderBy(n => n);
                        foreach (string item in query)
                        {
                            inputList.Remove(item);
                        }
                        break;
                    case "DC":
                        //destroys objects of players that have disconnected from the server
                        Destroy(playerList[int.Parse(splitData[1])]);
                        playerList.Remove(int.Parse(splitData[1]));
                        break;
                }

                break;
        }
	}

    public void Connect()
    {
        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", 0, 0, out error);
        if ((NetworkError)error == NetworkError.Ok) //only run if connection was succesful
        {
            Debug.Log("Connected to Server. Connection ID: " + connectionId);
            Instantiate(playerPrefab, transform.position, transform.rotation);
        }
        else //report error on failure
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
}
