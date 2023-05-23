using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IDataModality 
{
    void InitializeRecording(int numRecordings);
    void UpdateRecording(int timestep);
    Task CloseRecording(int recordingID);
}
