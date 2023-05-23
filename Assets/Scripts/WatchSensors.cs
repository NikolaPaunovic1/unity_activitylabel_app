using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class WatchSensors : IDataModality
{
    public List<WatchSensorHolder> Data { get; private set; }

    int _recID;
    WatchClient _client;
    long[] accTS = new long[100];
    long[] gyrTS = new long[100];
    long[] rotTS = new long[100];
    float[] accBuf = new float[100 * 3];
    float[] gyrBuf = new float[100 * 3];
    float[] rotBuf = new float[100 * 3];


    public WatchSensors(WatchClient client)
    {
        _client = client;
    }

    public async Task CloseRecording(int recordingID)
    {
        var recordedData = Data.ToList().Take(_recID).ToArray();
        var dataStr = StorageUtil.SerializeContainer(recordedData);
        StorageUtil.PersistStringToDisc(dataStr, $"WatchSensors_{recordingID}.txt");
    }

    public void InitializeRecording(int numRecordings)
    {
        Data = new List<WatchSensorHolder>(numRecordings);
        for (int i = 0; i < numRecordings; i++)
        {
            Data.Add(new WatchSensorHolder());
        }
    }

    public void UpdateRecording(int recID)
    {
        UpdateMostRecentData(recID);
    }

    public void UpdateMostRecentData(int recID)
    {
        _recID = recID;
        var (acc, gyr, acc_ts, gyr_ts) = _client.GetMostRecentData();
        Data[recID].AddSingleDataPoint(recID, acc, gyr, acc_ts, gyr_ts);
    }

    public void UpdateAllRecentData(int recID)
    {
        _recID = recID;
        var (accMsgLen, gyrMsgLen, rotMsgLen) = _client.GetData(ref accTS, ref accBuf, ref gyrTS, ref gyrBuf, ref rotTS, ref rotBuf);
        Data[recID].AddData(recID, accMsgLen, accTS, accBuf, gyrMsgLen, gyrTS, gyrBuf, rotMsgLen, rotTS, rotBuf);
    }
}
