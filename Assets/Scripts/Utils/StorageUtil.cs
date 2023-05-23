using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;


static public class StorageUtil
{
    private static string _baseFolder = null;
    private static string _currentRecFolder = null;


    static public string SerializeContainer(object container)
    {
        var JsonSerializerSettings = new JsonSerializerSettings();
        JsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        string output = JsonConvert.SerializeObject(container, JsonSerializerSettings);
        return output;
    }

    static public void PersistStringToDisc(string input, string fileName)
    {
        Debug.Log($"Entering PersistToDisc on filename: {fileName}");
        if (_currentRecFolder == null)
            CreateNewRecordingFolder();

        string file = Path.Combine(_currentRecFolder, fileName);
        using (StreamWriter writer = new StreamWriter(file))
        {
            // Write the text to the file
            writer.Write(input);
        }
    }

    static public string GetBaseStorageFolder()
    {
        if (_baseFolder == null)
        {
            _baseFolder = Path.Combine(Application.persistentDataPath, "WatchActionServer");
        }
        if (!Directory.Exists(_baseFolder))
        {
            Directory.CreateDirectory(_baseFolder);
        }

        return _baseFolder;
    }

    static public void CreateNewRecordingFolder()
    {
        if (_baseFolder == null)
        {
            _baseFolder = GetBaseStorageFolder();
        }

        int folderCount = GetFolderCount(_baseFolder);
        _currentRecFolder = Path.Combine(_baseFolder, $"rec_{folderCount+1}");
        // Create the folder if it doesn't exist
        if (!Directory.Exists(_currentRecFolder))
        {
            Directory.CreateDirectory(_currentRecFolder);
            Debug.Log("Folder created at: " + _currentRecFolder);
        }
        else
        {
            Debug.Log("Folder already exists at: " + _currentRecFolder);
        }
    }

    static private int GetFolderCount(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.LogError("Folder does not exist: " + path);
            return 0;
        }

        string[] folders = Directory.GetDirectories(path);
        int folderCount = folders.Length;
        return folderCount;
    }
}
