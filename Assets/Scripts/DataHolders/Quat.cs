using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Quat 
{
    public float x, y, z, w;

    public Quat(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
}
