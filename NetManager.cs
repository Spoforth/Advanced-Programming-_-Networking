using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetManager : MonoBehaviour {

    public GameObject[] spawnObjects;
    public GameObject playerSpawn;

    ConnectionConfig config;

    public int maxPlayers;
    public int roomName;

    int connectionID;
    int hostID;
    int sockedID;
    int socketPort;

	void Start()
    {
        NetworkTransport.Init();
        config = new ConnectionConfig();
        int channelID = config.AddChannel(QosType.ReliableSequenced);
    }

    void Update()
    {

    }

    void createRoom()
    {
        HostTopology topology = new HostTopology(config, maxPlayers);
        hostID = NetworkTransport.AddHost(topology, 0);
    }

    void joinRoom()
    {

    }

}
