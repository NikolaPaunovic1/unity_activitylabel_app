using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchSensorHolder
{
    public int recID;
    public long[] accTS;
    public long[] gyroTS;
    public long[] rotTS;
    public Vec3[] acc;
    public Vec3[] gyro;
    public Vec3[] rot;

    public WatchSensorHolder()
    {

    }

    public void AddData(int recID, int accMsgLen, long[] accTS, float[] accData, int gyrMsgLen,  long[] gyrTS, float[] gyrData, int rotMsgLen, long[] rotTS, float[] rotData)
    {
        this.recID = recID;
        this.accTS = new long[accMsgLen];
        this.gyroTS = new long[gyrMsgLen];
        this.rotTS = new long[rotMsgLen];
        Array.Copy(accTS, this.accTS, accMsgLen);
        Array.Copy(gyrTS, this.gyroTS, gyrMsgLen);
        Array.Copy(rotTS, this.rotTS, rotMsgLen);

        acc = new Vec3[accMsgLen];
        gyro = new Vec3[gyrMsgLen];
        rot = new Vec3[rotMsgLen];
        

        for(int i = 0; i < accMsgLen; i++)
            acc[i] = new Vec3(accData[i*3], accData[i*3 + 1], accData[i*3 + 2]);
        
        for (int i = 0; i < gyrMsgLen; i++)
            gyro[i] = new Vec3(gyrData[i*3], gyrData[i*3 + 1], gyrData[i*3 + 2]);

        for (int i = 0; i < rotMsgLen; i++)
            rot[i] = new Vec3(rotData[i * 3], rotData[i * 3 + 1], rotData[i * 3 + 2]);
    }

    public void AddSingleDataPoint(int recID, Vector3 acc, Vector3 gyr, long acc_ts, long gyr_ts)
    {
        this.recID = recID;
        this.acc = new Vec3[1];
        this.gyro = new Vec3[1];
        this.rot = new Vec3[1];
        this.accTS = new long[1];
        this.gyroTS = new long[1];
        this.rotTS = new long[1];

        this.acc[0] = new Vec3(acc.x, acc.y, acc.z);
        this.gyro[0] = new Vec3(gyr.x, gyr.y, gyr.z);
        this.rot[0] = new Vec3();
        this.accTS[0] = acc_ts;
        this.gyroTS[0] = gyr_ts;
        this.rotTS[0] = -1;
    }
}
