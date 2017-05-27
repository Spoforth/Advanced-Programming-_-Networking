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
    int unreliableChannelId;
    int hostId;
    int myPlayerID;
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
        unreliableChannelId = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology, 0);
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
                MessageGeneric message = MessageConverter.stringToMessage(msg);
                switch (message.getMessageID())
                {
                    case "PLAYERID":
                        myPlayerID = int.Parse(message.getDataAt(0));
                        break;
                    case "PLAYERSETUP":
                        //spawn all players at their positions
                        int _playerID = int.Parse(message.getDataAt(0));
                        float playerX = float.Parse(message.getDataAt(1));
                        float playerY = float.Parse(message.getDataAt(2));
                        float playerZ = float.Parse(message.getDataAt(3));
                        float playerRotX = float.Parse(message.getDataAt(4));
                        float playerRotY = float.Parse(message.getDataAt(5));
                        if (_playerID == myPlayerID) //spawns player object
                        {
                            GameObject playerOBJ = Instantiate(playerPrefab, new Vector3(playerX, playerY, playerZ), new Quaternion(playerRotX, playerRotY, 0, 0));
                            playerList.Add(myPlayerID, playerOBJ);
                        }
                        else //spawns other players
                        {
                            GameObject newPlayer = Instantiate(otherPlayers, new Vector3(playerX, playerY, playerZ), new Quaternion(playerRotX, playerRotY, 0, 0));
                            playerList.Add(_playerID, newPlayer);
                        }
                        break;
                    case "NEWPLAYER":
                        //spawns a new player when they connect to the server
                        if (int.Parse(message.getDataAt(0)) == myPlayerID)
                        {
                            break;
                        }
                        else
                        {
                            Debug.Log("Spawning new player");
                            GameObject newPlayer = Instantiate(otherPlayers, new Vector3(float.Parse(message.getDataAt(1)), float.Parse(message.getDataAt(2)), float.Parse(message.getDataAt(3))), new Quaternion(float.Parse(message.getDataAt(4)), float.Parse(message.getDataAt(5)), 0, 0));
                            playerList.Add(int.Parse(message.getDataAt(0)), newPlayer);
                        }
                        break;
                    case "UPDATE":
                        //updates the positions of each player that isn't the client
                        for(int i = 1; i < message.getSize(); i++)
                        {
                            //Debug.Log(splitData[i]);
                            //0 is player ID
                            //1 is position x
                            //2 is position y
                            //3 is position z
                            //4 is rotation x
                            //5 is rotation y
                            //6 is health
                            //7 is bool isDead
                            //8 is bool isFiring
                            int playerID = int.Parse(message.getDataAt(0));
                            if (playerID == myPlayerID) //if this position is the clients, skip the iteration
                            {
                                health = int.Parse(message.getDataAt(6));
                                continue;
                            }
                            GameObject obj = playerList[playerID];
                            Vector3 oldPos = playerList[playerID].transform.position;
                            Vector3 oldRot = playerList[playerID].transform.eulerAngles;
                            Vector3 pos =  new Vector3(float.Parse(message.getDataAt(1)), float.Parse(message.getDataAt(2)), float.Parse(message.getDataAt(3))); //get x y z position from data sent
                            obj.transform.position = Vector3.Lerp(oldPos, pos, 0.1f); //lerp to new position 
                            obj.transform.rotation = Quaternion.FromToRotation(oldRot, new Vector3(0, float.Parse(message.getDataAt(4)), 0)); //rotate body
                            obj.transform.GetChild(0).transform.rotation = Quaternion.FromToRotation(oldRot, new Vector3(float.Parse(message.getDataAt(5)), 0, 0)); //rotate head
                        }
                        break;
                    case "INPUTPROCESSED":
                        //checks which inputs have been processed by the server and updates the player's position acoording to that
                        Vector3 newPos = Vector3.zero;
                        Vector3 newRot = Vector3.zero;
                        for (int i = 0; i < inputList.Count; i++)
                        {
                            string[] inputArray = inputList[i].Split('|');
                            if (int.Parse(message.getDataAt(0)) < int.Parse(inputArray[0])) //these inputs were processed by the server, but are not the most recent
                            {
                                continue;
                            }
                            else if (int.Parse(message.getDataAt(0)) == int.Parse(inputArray[0])) //most recent input processed, gets new position data 
                            {
                                newPos = new Vector3(float.Parse(message.getDataAt(1)), float.Parse(message.getDataAt(2)), float.Parse(message.getDataAt(3)));
                                newRot = new Vector3(float.Parse(message.getDataAt(4)), float.Parse(message.getDataAt(5)), 0);
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
                        IEnumerable<string> query = inputList.Where(x => int.Parse(x[0].ToString()) <= int.Parse(message.getDataAt(0))).OrderBy(n => n);
                        foreach (string item in query)
                        {
                            inputList.Remove(item);
                        }
                        break;
                    case "DC":
                        //destroys objects of players that have disconnected from the server
                        Destroy(playerList[int.Parse(message.getDataAt(0))]);
                        playerList.Remove(int.Parse(message.getDataAt(0)));
                        break;
                }

                break;
        }
	}

    public void Connect()
    {
        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", 8888, 0, out error);
        if ((NetworkError)error == NetworkError.Ok) //only run if connection was succesful
        {
            Debug.Log("Connected to Server. Connection ID: " + connectionId);
            UICanvas.enabled = false;
        }
        else //report error on failure
        {
            Debug.LogError("NETWORK ERROR CODE:" + error.ToString());
        }
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
