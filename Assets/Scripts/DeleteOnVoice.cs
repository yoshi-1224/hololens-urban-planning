using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

/// <summary>
/// This component can be attached to make a game object deletable using a voice command.
/// </summary>
public class DeleteOnVoice : MonoBehaviour, ISpeechHandler {

    public const string COMMAND_DELETE = "delete";
    public event Action<DeleteOnVoice> OnBeforeDelete = delegate { };

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
        OnBeforeDelete.Invoke(this);
        Destroy(gameObject);
    }
}
