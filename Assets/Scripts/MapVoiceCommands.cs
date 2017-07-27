using UnityEngine;
using HoloToolkit.Unity.InputModule;

/// <summary>
/// Voice command Listener component that allows the map to respond to the voice commands
/// ONLY WHEN the map is in focus.
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
        }
    }

    private void setUpStreetView() {
        StreetViewManager.Instance.SetUpStreetView();
        InteractibleMap.Instance.hideMapTools();
    }

    private void pinGazedLocation() {
        PinnedLocationManager.Instance.pinLocation(GazeManager.Instance.HitPosition);
    }

}
