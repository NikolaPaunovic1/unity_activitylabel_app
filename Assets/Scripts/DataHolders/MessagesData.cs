using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MessagesData 
{
    public string Message { get; set; }
    public int Timestamp { get; set; }

    public MessagesData(string message, int recTS)
    {
        Message = message;
        Timestamp = recTS;
    }
}
