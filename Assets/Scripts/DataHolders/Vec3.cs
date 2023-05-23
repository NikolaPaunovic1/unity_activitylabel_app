using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Vec3
{
    public float x, y, z;
    public Vec3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static float Distance(Vec3 a, Vec3 b) => new Vec3(a.x - b.x, a.y - b.y, a.z - b.z).Magnitude();
    public float MagnitudeSqrd() => Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2);
    public float Magnitude() => Mathf.Sqrt(MagnitudeSqrd());
    public Vec3 Difference(Vec3 other) => new Vec3(x - other.x, y - other.y, z - other.z);
    public float Distance(Vec3 other) => this.Difference(other).Magnitude();

}
