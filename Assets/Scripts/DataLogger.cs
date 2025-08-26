using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class DataLogger : MonoBehaviour
{
    [SerializeField, Min(0)] public int subjectID = 0;
  
    private FileStream EventStream;
    private StreamWriter EventWriter;
    private string LogPath = "SessionData";
    private Queue<string> PendingEvents = new Queue<string>();
    private bool Closing;

    private void Start() {
        StartLogging();
    }
    
    private void StartLogging() {
        OpenNewEventLogFile();
        StartCoroutine(WritePendingEvents());
        
        Actions.OnEvent += OnEvent;
        Actions.OnQuit += OnQuit;
    }

    private void OpenNewEventLogFile() {
        if (!Directory.Exists($"{LogPath}/{subjectID}"))
            Directory.CreateDirectory($"{LogPath}/{subjectID}");
        
        string basePath = $"{LogPath}/{subjectID}/{DateTime.Now:yyyyMMdd-HHmmss}";
        EventStream = new FileStream(basePath + "_events.csv", FileMode.CreateNew);
        EventWriter = new StreamWriter(EventStream, Encoding.UTF8);

        string headers = "\"Timestamp\", \"Description\"";
        EventWriter.WriteLine(headers);
        OnEvent("Subject ID: " + subjectID);
    }

    private void OnEvent(string Description) {
        PendingEvents.Enqueue($"{DateTime.Now:hh:mm:ss:fff}, \"{Description}\"");
    }

    private IEnumerator WritePendingEvents() {
        while (true) {
            if (Closing && PendingEvents.Count == 0)
                break;
            
            if(PendingEvents.Count > 0)
                EventWriter.WriteLine(PendingEvents.Dequeue());

            yield return null;
        }
        
        EventWriter.Flush();
        yield return new WaitForSeconds(2f);
        EventWriter.Close();
        EventStream.Close();
        
        Quit();
    }

    private void OnQuit() {
        Closing = true;
    }
    
    private void Quit() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnDestroy() {
        Actions.OnEvent -= OnEvent;
        Actions.OnQuit -= OnQuit;
    }
}