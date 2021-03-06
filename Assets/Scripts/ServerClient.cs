﻿using System.Collections;
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
    int unreliableChannelId;
    int hostId;
    public Transform rayOrigin;
    public bool firing;
    public Rigidbody rb;
    int inputCount = 0; //how many inputs have been sent
    int lastInputProcessed = 0;

    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelId = config.AddChannel(QosType.Reliable);
        unreliableChannelId = config.AddChannel(QosType.Unreliable);
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

    public override void Turn(float xRot, float yRot)
    {
        transform.Rotate(0, yRot, 0);
        Transform headTransform = transform.GetChild(0);
        headTransform.Rotate(-xRot, 0, 0);
        headTransform.localRotation = Quaternion.Euler(headTransform.eulerAngles.x, 0, 0);
    }

    public void runInput(byte[] packet)
    {
        string inputString = Encoding.Unicode.GetString(packet);
        MessageGeneric inputMessage = MessageConverter.stringToMessage(inputString);
        switch (inputMessage.getMessageID())
        {
            case "MOVE":
                Move(float.Parse(inputMessage.getDataAt(0)), float.Parse(inputMessage.getDataAt(1)));
                break;
            case "SHOOT":
                Shoot();
                break;
            case "TURN":
                Turn(float.Parse(inputMessage.getDataAt(1)), float.Parse(inputMessage.getDataAt(0)));
                break;
        }
        inputCount++;
    }

    public void updatePos()
    {
        //run through all inputs that server has recieved
        //send result of inputs and last input that was processed to the player this object is responsible for
        MessageGeneric message = new MessageGeneric(6, "INTPUTPROCESSED");
        message.setDataAt(0, inputCount.ToString());
        message.setDataAt(1, transform.position.x.ToString());
        message.setDataAt(2, transform.position.y.ToString());
        message.setDataAt(3, transform.position.z.ToString());
        message.setDataAt(4, transform.rotation.x.ToString());
        message.setDataAt(5, transform.rotation.y.ToString());
        server.SendToPlayer(MessageConverter.messageToString(message), reliableChannelId, ConnectionID);
        lastInputProcessed = inputCount;
        //empties queue
    }
}
