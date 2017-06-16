using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class DeleteOnVoice : MonoBehaviour, ISpeechHandler {
    public const string COMMAND_DELETE = "delete";

    // put Update() just for the sake of being able to disable this component
    // from Unity editor
    private void Update() {
        
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch(eventData.RecognizedText.ToLower()) {
            case COMMAND_DELETE:
                deleteThisObject();
                break;
            default:
                // ignore
                break;
        }
    }

    private void deleteThisObject() {
        Destroy(gameObject);
    }
}
