using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataManager : MonoBehaviour
{
    [SerializeField] Image _recordingFeedback;
    [SerializeField] ActivityLogger _activityLogger;
    [SerializeField] WatchClient _watchClient;
    WatchSensors _watchSensors;
    static public int RECFPS { get; private set; } = 40;
    bool _isRecording = false;

    int _recTS = 0;

    void Start()
    {
        _watchSensors = new WatchSensors(_watchClient);
        Time.fixedDeltaTime = 1 / (float)RECFPS;  // _fps update rate
        _isRecording = false;
        _recordingFeedback.color = Color.red;
    }

    public void StartRecording()
    {
        _recTS = 0;
        var numRecs = 20 * 60 * RECFPS;  // 20 minutes, 60 seconds
        _watchSensors.InitializeRecording(numRecs);
        _activityLogger.InitializeRecording(numRecs);
        _recordingFeedback.color = Color.green;
        _isRecording = true;
    }

    public async void StopRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;
        StorageUtil.CreateNewRecordingFolder();
        await _watchSensors.CloseRecording(_recTS);
        await _activityLogger.CloseRecording(_recTS);
        _recordingFeedback.color = Color.red;
    }

    void FixedUpdate()
    {
        if (!_isRecording) return;

        _recTS++;
        _watchSensors.UpdateRecording(_recTS);
        _activityLogger.UpdateRecording(_recTS);
    }
}
