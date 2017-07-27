using UnityEngine;
using System;
using HoloToolkit.Unity.InputModule;

/// <summary>
/// This component can be attached to a game object in order to make it movable with voice command. Note that GestureManager is required in the scene.
/// </summary>
public class Movable : MonoBehaviour, ISpeechHandler {
    [SerializeField]
    private float TranslationSensitivity = 10f;
    private Vector3 previousManipulationPosition;

    public const string COMMAND_MOVE = "position";
    public event Action OnRegisteringForTranslation = delegate { };
    public event Action OnUnregisterForTranslation = delegate { };
    public event Action OnPositionUpdated = delegate { };

    public void RegisterForTranslation() {
        if (GestureManager.Instance == null)
            return;

        GestureManager.Instance.RegisterGameObjectForTranslation(this);
        OnRegisteringForTranslation.Invoke();
    }

    public void PerformTranslationStarted(Vector3 cumulativeDelta) {
        previousManipulationPosition = cumulativeDelta;
    }

    public void PerformTranslationUpdate(Vector3 cumulativeDelta) {
        Vector3 moveVector = Vector3.zero;
        moveVector = cumulativeDelta - previousManipulationPosition;
        previousManipulationPosition = cumulativeDelta;
        
        // disable the y-move as it doesn't make sense to have buildings flying around
        transform.position += new Vector3(moveVector.x * TranslationSensitivity, 0, moveVector.z * TranslationSensitivity);
    }

    public void UnregisterForTranslation() {
        OnUnregisterForTranslation.Invoke();
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        if (eventData.RecognizedText.ToLower().Equals(COMMAND_MOVE)) {
            RegisterForTranslation();
        }
    }
}
