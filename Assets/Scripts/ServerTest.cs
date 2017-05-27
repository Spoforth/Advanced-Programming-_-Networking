using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerTest : MonoBehaviour {

    public GameObject playerObject;

    int connectionId;
    int maxConnections = 10;
    int reliableChannelId;
    int unreliableChannelId;
    int hostId;
    int socketPort = 8888;
    int playerCount;
    private IEnumerator coroutine;
    public float respawnTime = 3f;
    public Dictionary<int, GameObject> clients = new Dictionary<int, GameObject>();
    public Transform[] spawnPoints;
    
    void Start()
    {
        //NETWORK SETUP
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelId = config.AddChannel(QosType.Reliable); ;
        unreliableChannelId = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostId = NetworkTransport.AddHost(topology, socketPort, null); // null means anyone can join, used for servers
        Debug.Log("Socket Open. Host ID is " + hostId);
        //server timestep, updates positions and sends them every tick
        coroutine = ExecuteAfterTime(0.1f);
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
                Debug.Log("Connection event recieved. recHostID: " + recHostId + " recConnedID: " + recConnectionId);
                playerCount++;
                int newPlayerID = playerCount;
                MessageGeneric playeridMessage = new MessageGeneric(1, "PLAYERID");
                playeridMessage.setDataAt(0, newPlayerID.ToString());
                SendToPlayer(MessageConverter.messageToString(playeridMessage), reliableChannelId, recConnectionId);
                int index = Random.Range(0, spawnPoints.Length);
                GameObject clone = Instantiate(playerObject, spawnPoints[index].position, spawnPoints[index].rotation);
                clients.Add(playerCount, clone); //add new player object to dictionary
                ServerClient cloneSC = clone.GetComponent<ServerClient>();
                cloneSC.ConnectionID = recConnectionId; //tell the ServerClient which person they're responsible for and who to send their data to.
                foreach (KeyValuePair<int, GameObject> c in clients)
                {
                    MessageGeneric message = new MessageGeneric(6, "PLAYERSETUP");
                    message.setDataAt(0, c.Key.ToString());
                    message.setDataAt(1, c.Value.transform.position.x.ToString());
                    message.setDataAt(2, c.Value.transform.position.y.ToString());
                    message.setDataAt(3, c.Value.transform.position.z.ToString());
                    message.setDataAt(4, c.Value.transform.rotation.x.ToString());
                    message.setDataAt(5, c.Value.transform.rotation.y.ToString());
                    SendToPlayer(MessageConverter.messageToString(message), reliableChannelId, recConnectionId);
                }
                MessageGeneric newPlayerMsg = new MessageGeneric(6, "NEWPLAYER");
                newPlayerMsg.setDataAt(0, newPlayerID.ToString());
                newPlayerMsg.setDataAt(1, clone.transform.position.x.ToString());
                newPlayerMsg.setDataAt(2, clone.transform.position.y.ToString());
                newPlayerMsg.setDataAt(3, clone.transform.position.z.ToString());
                newPlayerMsg.setDataAt(4, clone.transform.rotation.x.ToString());
                newPlayerMsg.setDataAt(5, clone.transform.rotation.y.ToString());
                Send(MessageConverter.messageToString(newPlayerMsg), reliableChannelId);
                break;
            case NetworkEventType.DataEvent:
                int dataPlayerID = 0;
                foreach (KeyValuePair<int, GameObject> dataPlayer in clients)
                {
                    ServerClient sc = dataPlayer.Value.GetComponent<ServerClient>();
                    if (sc.ConnectionID == recConnectionId)
                    {
                        dataPlayerID = dataPlayer.Key;
                    }
                }
                GameObject playerOBJ = clients[dataPlayerID];
                ServerClient player = playerOBJ.GetComponent<ServerClient>();
                player.runInput(recBuffer); //input updates handled by ServerClient, add them to queue so that no inputs are lost
                break;
            case NetworkEventType.DisconnectEvent:
                //destroy player on disconnect, tell everyone to do the same
                int dcPlayerID = 0;
                foreach (KeyValuePair<int, GameObject> dcPlayer in clients)
                {
                    ServerClient sc = dcPlayer.Value.GetComponent<ServerClient>();
                    if (sc.ConnectionID == recConnectionId)
                    {
                        dcPlayerID = dcPlayer.Key;
                    }
                }
                Destroy(clients[dcPlayerID]);
                clients.Remove(dcPlayerID);
                MessageGeneric dcMessage = new MessageGeneric(2, "DC");
                dcMessage.setDataAt(1, dcPlayerID.ToString());
                Send(MessageConverter.messageToString(dcMessage), reliableChannelId);
                break;
        }
    }

    void FixedUpdate()
    {

    }

    public void Send(string message, int channelID) //send message to every player in clients
    {
        byte error;
        Debug.Log("Sending: " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (KeyValuePair<int, GameObject> entry in clients)
        {
            NetworkTransport.Send(hostId, entry.Key, channelID, msg, message.Length * sizeof(char), out error);
        }
    }

    public void SendToPlayer(string message, int channelID, int cnID) //send message to specific player
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

    IEnumerator ExecuteAfterTime(float time) //update player data and send it to each player
    {
        while (true)
        {
            foreach (KeyValuePair<int, GameObject> entry in clients)
            {
                MessageGeneric message = new MessageGeneric(9, "UPDATE");
                GameObject obj = entry.Value;
                ServerClient sc = obj.GetComponent<ServerClient>();
                sc.updatePos();
                //insert characters to split string so each value is easier to track
                message.setDataAt(0, entry.Key.ToString());
                message.setDataAt(1, obj.transform.position.x.ToString());
                message.setDataAt(2, obj.transform.position.y.ToString());
                message.setDataAt(3, obj.transform.position.z.ToString());
                message.setDataAt(4, obj.transform.rotation.x.ToString());
                message.setDataAt(5, obj.transform.rotation.y.ToString());
                message.setDataAt(6, sc.health.ToString());
                message.setDataAt(7, sc.isDead.ToString());
                message.setDataAt(8, sc.firing.ToString());
                Send(MessageConverter.messageToString(message), unreliableChannelId);
            }
            yield return new WaitForSeconds(time);
        }
    }

    IEnumerator SpawnPlayer(int playerID)
    {
        yield return new WaitForSeconds(respawnTime); //wait for respawn timer to expire
        int index = Random.Range(0, spawnPoints.Length); //pick random spawn point
        GameObject player = clients[playerID];
        player.transform.position = spawnPoints[index].position; //move player to their new position
        ServerClient sc = player.GetComponent<ServerClient>();
        sc.isDead = false; //tell them they're no longer dead
        sc.health = sc.maxHealth;
        player.transform.GetChild(0).gameObject.SetActive(true); //turn on renderers
        player.GetComponent<CapsuleCollider>().enabled = true;
    }
}
