using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour, fpsController {
    int _health;
    bool _isDead;

    public int health
    {
        get
        {
            return _health;
        }
        set
        {
            _health = value;
        }
    }

    public bool isDead
    {
        get
        {
            return _isDead;
        } 
        set
        {
            _isDead = value;
        }
    }

    public virtual void Move(float xMov, float zMov)
    {
        transform.Translate(new Vector3(xMov, 0, zMov));
    }

    public virtual void Shoot()
    {
        StopCoroutine("fireLaser");
        StartCoroutine("fireLaser");
    }

    public virtual void Turn(float xRot, float yRot)
    {
        transform.Rotate(new Vector3(xRot, yRot));
    }
}
