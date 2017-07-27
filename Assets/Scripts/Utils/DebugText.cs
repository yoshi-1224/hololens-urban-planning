using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;

[RequireComponent(typeof(TextMesh))]
[RequireComponent(typeof(HandDraggable))]
/// <summary>
/// listens to Unity Debug messages and displays in the textMesh
/// Make sure to register the voice commands in SpeechInputSource prefab
/// </summary>
public class DebugText : Singleton<DebugText>, ISpeechHandler {
    [Tooltip("Whether or not to show the stacktrace along with the debug message")]
    public bool showStackTrace = true;

    [SerializeField]
    public List<LogType> LogTypesToShow;
    private TextMesh textMeshCache;

    private const string COMMAND_CLEAR_LOG = "clear log";
    private const string COMMAND_SHOW_STACK = "show stack";
    private const string COMMAND_HIDE_STACK = "hide stack";

    BoxCollider colliderSingleton;

    private void Start() {
        textMeshCache = GetComponent<TextMesh>();
        Application.logMessageReceived += writeLogToTextMesh;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Application.logMessageReceived -= writeLogToTextMesh;
    }

    public void WriteToText(string text) {
        textMeshCache.text += text;
    }

    private void writeLogToTextMesh(string logString, string stackTrace, LogType type) {
        if (!LogTypesToShow.Contains(type))
            return;
        textMeshCache.text += "<color=red>" + logString + "</color>" + "\n";
        if (showStackTrace)
            textMeshCache.text += stackTrace + "\n";
        if (colliderSingleton != null) {
            Destroy(colliderSingleton);
            colliderSingleton = null;
        }

        if (colliderSingleton == null)
            colliderSingleton = gameObject.AddComponent<BoxCollider>();

    }

    private void clearLog() {
        textMeshCache.text = "Logger:\n";
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_CLEAR_LOG:
                clearLog();
                break;
            case COMMAND_SHOW_STACK:
                enableStackTrace();
                break;
            case COMMAND_HIDE_STACK:
                disableStackTrace();
                break;
            default:
                break;
        }
    }

    private void sendLog() {
        // send the text to a host using POST/GET etc?.
    }

    private void enableStackTrace() {
        showStackTrace = true;
    }

    private void disableStackTrace() {
        showStackTrace = false;
    }


}
