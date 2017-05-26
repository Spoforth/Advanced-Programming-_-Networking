using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class ServerClient : player{

    public delegate void Respawn(int connectionID);
    public event Respawn RespawnMe;
    public int maxHealth;
    public int damage;
    public int ConnectionID;
    public ServerTest server;
    int maxConnections = 10;
    int reliableChannelId;
    int hostId;
    public Transform rayOrigin;
    public bool firing;
    public Rigidbody rb;
    List<string> inputQueue = new List<string>();
    int inputCount = 0; //how many inputs have been sent
    int lastInputProcessed = 0;

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

    IEnumerator fireLaser()
    {
        while (firing == true)
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                if (hit.transform.tag == "Player")
                {
                    GameObject playerHit = hit.transform.gameObject;
                    ServerClient playerSC = playerHit.GetComponent<ServerClient>();
                    playerSC.GetShot(50); //deal damage
                }
            }
            yield return null;
        }
        firing = false;
    }

    public void GetShot(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            isDead = true;
            if (RespawnMe != null)
            {
                RespawnMe(ConnectionID);
            }
            transform.GetChild(0).gameObject.SetActive(false); //disable rendered objects
            GetComponent<CapsuleCollider>().enabled = false; //disable collider
        }

    }

    public void addToQueue(byte[] packet)
    {
        string inputString = Encoding.Unicode.GetString(packet);
        inputQueue.Add(inputString);
    }

    public void runQueue()
    {
        //run through all inputs that server has recieved
        string msg = ConnectionID + "|";
        for (int i = 0; i < inputQueue.Count; i++)
        {
            msg += inputQueue[i] + "|";
            string[] splitData = inputQueue[i].Split('|');
            switch (splitData[1])
            {
                case "MOVE":
                    Move(float.Parse(splitData[2]), float.Parse(splitData[3]));
                    msg += "MOVE%" + splitData[2] + "%" + splitData[3] + "|";
                    break;
                case "SHOOT":
                    Shoot();
                    msg += "SHOOT|";
                    break;
                case "TURN":
                    string[] turnData = splitData[2].Split('/');
                    Turn(float.Parse(turnData[0]), float.Parse(turnData[1]));
                    msg += turnData[0] + "%" + turnData[1] + "|";
                    break;
            }
        }
        //send result of inputs and last input that was processed to the player this object is responsible for
        server.SendToPlayer("INPUTPROCESSED|" + inputCount + "|" + transform.position.x + "/" + transform.position.y + "/" + transform.position.z + "|" + transform.rotation.x + "/" + transform.rotation.y + "|", ConnectionID, reliableChannelId);
        lastInputProcessed = inputCount;
        //gets all inputs that have been processed and removes them
        IEnumerable<string> query = inputQueue.Where(x => int.Parse(x[0].ToString()) <= inputCount).OrderBy(n => n);
        foreach (string item in query)
        {
            inputQueue.Remove(item);
        }
    }

    public bool checkInputNumber(string s)
    {
        return s.StartsWith(inputCount.ToString());
    }
}
