using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

/// <summary>
/// attach to the map so that this is available only when the map is gazed at
/// </summary>
public class MapVoiceCommands : MonoBehaviour, ISpeechHandler {

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case StreetViewManager.COMMAND_STREET_VIEW:
                setUpStreetView();
                break;
            case PinnedLocationManager.COMMAND_PIN_LOCATION:
                pinGazedLocation();
                break;

            default:
                //ignore
                break;
        }
    }

    private void setUpStreetView() {
        StreetViewManager.Instance.SetUpStreetView();
    }

    private void pinGazedLocation() {
        PinnedLocationManager.Instance.pinGazedLocation();
    }

}
