using System;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

/// <summary>
/// This component can be attached to a game object in order to make it rotatable with voice command. Note that GestureManager is required in the scene.
/// </summary>
public class Rotatable : MonoBehaviour, ISpeechHandler {
    [SerializeField]
    private float RotationSensitivity = 10f;

    public const string COMMAND_ROTATE = "rotate";
    public event Action OnRegisteringForRotation = delegate { };
    public event Action OnUnregisterForRotation = delegate { };
    public event Action OnRotationUpdated = delegate { };

    public void RegisterForRotation() {
        if (GestureManager.Instance == null)
            return;
        
        GestureManager.Instance.RegisterGameObjectForRotation(this);
        OnRegisteringForRotation.Invoke();
    }

    public void PerformRotationStarted(Vector3 cumulativeDelta) {
    }

    public void PerformRotationUpdate(Vector3 normalizedOffset) {
        float rotationFactor = -normalizedOffset.x * RotationSensitivity;
        transform.Rotate(new Vector3(0, rotationFactor, 0));
        OnRotationUpdated.Invoke();
    }

    public void UnregisterForRotation() {
        OnUnregisterForRotation.Invoke();
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        if (eventData.RecognizedText.ToLower().Equals(COMMAND_ROTATE)) {
            RegisterForRotation();
        }
    }
}
