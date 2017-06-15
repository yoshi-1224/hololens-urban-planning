using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class DeleteOnVoice : MonoBehaviour, ISpeechHandler {
    public const string COMMAND_DELETE = "delete";

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
