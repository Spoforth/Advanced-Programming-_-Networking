using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface fpsController{

    void Move(float xMov, float zMov);
    void Shoot();
    void Turn(float xRot, float yRot);
}
