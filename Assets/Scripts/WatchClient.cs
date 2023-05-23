using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class WatchClient : MonoBehaviour
{
    [SerializeField] GameObject _connectionFeedback;
    public event Action Connected;
    public event Action Disconnected;

    public bool HasNewDataArrived { get; private set; }
    public bool WasNewDataWritten { get; private set; }
    public int NewDataCount = 0;

    UdpClient _client;

    int _localPort = 6000;
    IPEndPoint _connectedEndpoint;

    string _connectionStr = "connect_watch".Trim();
    string _confirmStr = "confirm_connect_watch".Trim();

    private int numBufs = 12;
    private int bufDataLen = 64;
    private List<long[]> _accTS = new List<long[]>();
    private List<long[]> _gyrTS = new List<long[]>();
    private List<long[]> _rotTS = new List<long[]>();
    private List<float[]> _accBuf = new List<float[]>();
    private List<float[]> _gyrBuf = new List<float[]>();
    private List<float[]> _rotBuf = new List<float[]>();

    private int _currAccBuffIdx = 0;
    private int _currGyrBuffIdx = 0;
    private int _currRotBuffIdx = 0;

    private Vector3 _lastAcc;
    private Vector3 _lastGyr;
    private Vector3 _recentRot;

    private int _numDataInCurrentAccBuff = 0;
    private int _numDataInCurrentGyrBuff = 0;
    private int _numDataInCurrentRotBuff = 0;

    private long _lastAccTimestamp = -1;
    private long _lastGyroTimestamp = -1;
    private long _lastRotTimestamp = -1;


    private const int LONG_SIZE = sizeof(long);
    private const int FLOAT_SIZE = sizeof(float);

    Task _listeningThread = null;

    private string debugStr;

    CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private enum DataType { accel, gyro, rot};

    public (int, int, int) GetData(ref long[] accTS, ref float[] accBuff, ref long[] gyrTS, ref float[] gyrBuff, ref long[] rotTS, ref float[] rotBuff)
    {
        HasNewDataArrived = false;

        int accIdx, gyrIdx, rotIdx, accMsgLen, gyrMsgLen, rotMsgLen;

        accIdx = _currAccBuffIdx;  // read from prev list
        gyrIdx = _currGyrBuffIdx;
        rotIdx = _currRotBuffIdx;
        accMsgLen = _numDataInCurrentAccBuff;
        gyrMsgLen = _numDataInCurrentGyrBuff;
        rotMsgLen = _numDataInCurrentRotBuff;
        NextBuffers();

        if (WasNewDataWritten)
            NewDataCount++;

        Buffer.BlockCopy(this._accTS[accIdx], 0, accTS, 0, accMsgLen * LONG_SIZE);
        Buffer.BlockCopy(this._accBuf[accIdx], 0, accBuff, 0, accMsgLen * 3 * FLOAT_SIZE);
        Buffer.BlockCopy(this._gyrTS[gyrIdx], 0, gyrTS, 0, gyrMsgLen * LONG_SIZE);
        Buffer.BlockCopy(this._gyrBuf[gyrIdx], 0, gyrBuff, 0, gyrMsgLen * 3 * FLOAT_SIZE);
        Buffer.BlockCopy(this._rotTS[rotIdx], 0, rotTS, 0, rotMsgLen * LONG_SIZE);
        Buffer.BlockCopy(this._rotBuf[rotIdx], 0, rotBuff, 0, rotMsgLen * 3 * FLOAT_SIZE);

        return (accMsgLen, gyrMsgLen, rotMsgLen); 
    }

    public (Vector3, Vector3, long, long) GetMostRecentData()
    {
        HasNewDataArrived = false;
        return (_lastAcc, _lastGyr, _lastAccTimestamp, _lastGyroTimestamp);
    }


    public async void ListenForMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await ListenSingleMsg();
        }
    }

    public async void ListenForConnection()
    {
        while (true)
        {
            var msg = await _client.ReceiveAsync();
            string messagStr = Encoding.ASCII.GetString(msg.Buffer).Trim();
            Debug.Log(messagStr);

            if (messagStr.Equals(_connectionStr))
            {
                _connectedEndpoint = msg.RemoteEndPoint;

                Byte[] confirmBytes;
                confirmBytes = Encoding.ASCII.GetBytes(_confirmStr);
                _client.Send(confirmBytes, confirmBytes.Length, _connectedEndpoint);
                var rawImage = _connectionFeedback.GetComponent<RawImage>();
                rawImage.color = Color.green;
                Connected?.Invoke();
                break;
            }
        }
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        _listeningThread = Task.Run(() => ListenForMessages(cancellationToken), cancellationToken);
    }

    public void CloseConnection()
    {
        Disconnected?.Invoke();
    }

    private void Awake()
    {
        _client = new UdpClient(_localPort);
        _client.EnableBroadcast = true;
        ListenForConnection();
        var rawImage = _connectionFeedback.GetComponent<RawImage>();
        rawImage.color = Color.red;

        InitBuffers();
    }

    private void InitBuffers()
    {
        for(int i = 0; i < numBufs; i++)
        {
            _accTS.Add(new long[bufDataLen]);
            _gyrTS.Add(new long[bufDataLen]);
            _rotTS.Add(new long[bufDataLen]);
            _accBuf.Add(new float[3 * bufDataLen]);
            _gyrBuf.Add(new float[3 * bufDataLen]);
            _rotBuf.Add(new float[3 * bufDataLen]);
        }
    }

    private bool IsRot(string[] vals) => int.Parse(vals[0]) == 2;
    private bool IsGyro(string[] vals) => int.Parse(vals[0]) == 1;
    private bool IsAccel(string[] vals) => int.Parse(vals[0]) == 0;

    private bool WriteAccelData(string[] msg)
    {
        // check if buffers need to be changed to hold new data
        int numAccData = (msg.Length - 1) / 4;

        for (int i = 0; i < numAccData; i++)  // % 4 because msg_timeframe len = x, y, z, ts
        {
            var ts = long.Parse(msg[i * 4 + 4]);
            if (ts < _lastAccTimestamp) continue;  // first ts of new msg > last timestamp

            // Write most recent data point

            
            int dataIdx = _numDataInCurrentAccBuff;

            _accBuf[_currAccBuffIdx][dataIdx * 3] = float.Parse(msg[i * 4 + 1]);
            _accBuf[_currAccBuffIdx][dataIdx * 3 + 1] = float.Parse(msg[i * 4 + 2]);
            _accBuf[_currAccBuffIdx][dataIdx * 3 + 2] = float.Parse(msg[i * 4 + 3]);
            _accTS[_currAccBuffIdx][dataIdx] = ts;
            _lastAccTimestamp = ts;

            _numDataInCurrentAccBuff++;

            if (_numDataInCurrentAccBuff >= bufDataLen)
            {
                NextBuffers();
                return false;
            }     
        }
        return true;
    }

    private bool WriteGyroData(string[] msg)
    {
        int numFrames = (msg.Length - 1) / 4;  // -1 to get rid of first character ('1') marking this message as gyro message. Message per timestep: x, y, z ts
        
        for (int i = 0; i < numFrames; i++)  // % 4 because msg len = x, y, z, ts. len - 1 because first val is acc/gyro assign
        {
            var ts = long.Parse(msg[i * 4 + 4]);
            if (ts < _lastGyroTimestamp) continue;  // first ts of new msg > most current timestamp

            int msgIdx = _numDataInCurrentGyrBuff;

            _gyrBuf[_currGyrBuffIdx][msgIdx * 3] = float.Parse(msg[i * 4 + 1]);  // add +1 offset because msgVals idx 0 is reserved for acc/gyro msg type info
            _gyrBuf[_currGyrBuffIdx][msgIdx * 3 + 1] = float.Parse(msg[i * 4 + 2]); // also: first save +2 -> x and save +1 -> w at end.  
            _gyrBuf[_currGyrBuffIdx][msgIdx * 3 + 2] = float.Parse(msg[i * 4 + 3]);
            _gyrTS[_currGyrBuffIdx][msgIdx] = ts;
            _lastGyroTimestamp = ts;

            _numDataInCurrentGyrBuff++;

            if (_numDataInCurrentGyrBuff >= bufDataLen)
            {
                NextBuffers();
                return false;
            }
            //debugStr = $"{float.Parse(msg[i * 5 + 2])}, {float.Parse(msg[i * 5 + 3])}, {float.Parse(msg[i * 5 + 4])}, {float.Parse(msg[i * 5 + 1])}";
        }
        return true;
    }

    private bool WriteRotData(string[] msg)
    {
        int numFrames = (msg.Length - 1) / 4;  // -1 to get rid of first character ('1') marking this message as gyro message. Message per timestep: x, y, z ts

        for (int i = 0; i < numFrames; i++)  // % 4 because msg len = x, y, z, ts. len - 1 because first val is acc/gyro assign
        {
            var ts = long.Parse(msg[i * 4 + 4]);
            if (ts < _lastRotTimestamp) continue;  // first ts of new msg > most current timestamp

            int msgIdx = _numDataInCurrentRotBuff;

            _rotBuf[_currRotBuffIdx][msgIdx * 3] = float.Parse(msg[i * 4 + 1]);  // add +1 offset because msgVals idx 0 is reserved for acc/gyro msg type info
            _rotBuf[_currRotBuffIdx][msgIdx * 3 + 1] = float.Parse(msg[i * 4 + 2]); // also: first save +2 -> x and save +1 -> w at end.  
            _rotBuf[_currRotBuffIdx][msgIdx * 3 + 2] = float.Parse(msg[i * 4 + 3]);
            _rotTS[_currRotBuffIdx][msgIdx] = ts;
            _lastGyroTimestamp = ts;

            _numDataInCurrentRotBuff++;

            if (_numDataInCurrentRotBuff >= bufDataLen)
            {
                NextBuffers();
                return false;
            }
            //debugStr = $"{float.Parse(msg[i * 5 + 2])}, {float.Parse(msg[i * 5 + 3])}, {float.Parse(msg[i * 5 + 4])}, {float.Parse(msg[i * 5 + 1])}";
        }
        return true;
    }

    private void NextAccBuffer()
    {
        _currAccBuffIdx = (_currAccBuffIdx + 1) % numBufs;
        _numDataInCurrentAccBuff = 0;  
    }

    private void NextGyrBuffer()
    {
        _currGyrBuffIdx = (_currGyrBuffIdx + 1) % numBufs;
        _numDataInCurrentGyrBuff = 0;
    }

    private void NextRotBuffer()
    {
        _currRotBuffIdx = (_currRotBuffIdx + 1) % numBufs;
        _numDataInCurrentRotBuff = 0;
    }

    private void NextBuffers()
    {
        NextAccBuffer();
        NextGyrBuffer();
        NextRotBuffer();
    }

    public async Task ListenMultiMsgs()
    {
        try
        {
            var res = await _client.ReceiveAsync();
            string messagStr = Encoding.ASCII.GetString(res.Buffer);

            HasNewDataArrived = true;
            // acc and gyro part of message are split with #
            var accAndGyroAndRot = messagStr.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var msgData in accAndGyroAndRot)
            {
                // individual x,y,z,(w) components are split with ',' and multiple accel / gyro values are concattenated together: ex: x1, y1, z1, ts1, x2, y2, z2, ts2...
                var msg = msgData.Split(',');
                if (msg.Length == 1)  // sometimes "??vw10" is sent. This should be ignored.
                    continue;

                if (IsAccel(msg))
                    WriteAccelData(msg);
                else if (IsGyro(msg))
                    WriteGyroData(msg);
                else if (IsRot(msg))
                    WriteRotData(msg);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Rcv exception: {e.Message}");
            return;
        }
    }

    public async Task ListenSingleMsg()
    {
        try
        {
            var res = await _client.ReceiveAsync();
            string messagStr = Encoding.ASCII.GetString(res.Buffer);

            HasNewDataArrived = true;
            
            var msgElements = messagStr.Split(',');
            _lastAcc = new Vector3(float.Parse(msgElements[0]), float.Parse(msgElements[1]), float.Parse(msgElements[2]));
            _lastAccTimestamp = long.Parse(msgElements[3]);
            _lastGyr = new Vector3(float.Parse(msgElements[4]), float.Parse(msgElements[5]), float.Parse(msgElements[6]));
            _lastGyroTimestamp = long.Parse(msgElements[7]);

        }
        catch (Exception e)
        {
            Debug.Log($"Rcv exception: {e.Message}");
            return;
        }
    }

    ~WatchClient()
    {
        _client?.Close();
        _client?.Dispose();
        Debug.Log("Destrucor WatchClient");
        _listeningThread?.Dispose();
    }
}
