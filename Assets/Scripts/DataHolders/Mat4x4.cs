using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.Serialization;

public struct Mat4x4
{
    public float[] data;

    [JsonIgnore]
    readonly float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;


    public Mat4x4(Matrix4x4 data)
    {
        this.m00 = data.m00;
        this.m01 = data.m01;
        this.m02 = data.m02;
        this.m03 = data.m03;
        this.m10 = data.m10;
        this.m11 = data.m11;
        this.m12 = data.m12;
        this.m13 = data.m13;
        this.m20 = data.m20;
        this.m21 = data.m21;
        this.m22 = data.m22;
        this.m23 = data.m23;
        this.m30 = data.m30;
        this.m31 = data.m31;
        this.m32 = data.m32;
        this.m33 = data.m33;

        this.data = null;
    }

    [OnSerializing]
    internal void OnSerializingMethod(StreamingContext context)
    {
        data = new float[]
        {
            m00, m01, m02, m03,
            m10, m11, m12, m13,
            m20, m21, m22, m23,
            m30, m31, m32, m33,
        };
    }
}
