using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ActivityLogger : MonoBehaviour, IDataModality
{
    List<object> _activities;
    int _recTS;

    public async Task CloseRecording(int recordingID)
    {
        var recordedData = _activities.ToArray();
        var dataStr = StorageUtil.SerializeContainer(_activities);
        StorageUtil.PersistStringToDisc(dataStr, $"ActivityLabels_{recordingID}.txt");
    }

    public void InitializeRecording(int numRecordings)
    {
        _recTS = 0;
        _activities = new List<object>();
    }

    public void UpdateRecording(int timestep)
    {
        _recTS = timestep;
    }

    public void AddActivity(string activityName)
    {
        var data = new { _recTS, activityName };
        _activities.Add(data);
    }

}
