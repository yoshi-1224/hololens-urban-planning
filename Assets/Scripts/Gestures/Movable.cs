using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Movable : MonoBehaviour {
    [SerializeField]
    private float TranslationSensitivity = 10f;
    private Vector3 previousManipulationPosition;

    public const string COMMAND_MOVE = "position";
    public event Action OnRegisteringForTranslation = delegate { };
    public event Action OnUnregisterForTranslation = delegate { };
    public event Action OnPositionUpdated = delegate { };

    public void RegisterForTranslation() {
        GestureManager.Instance.RegisterGameObjectForTranslation(this);
        OnRegisteringForTranslation.Invoke();
    }

    public void PerformTranslationStarted(Vector3 cumulativeDelta) {
        // here doesn't have to be adjusted to camera space
        previousManipulationPosition = cumulativeDelta;
    }

    public void PerformTranslationUpdate(Vector3 cumulativeDelta) {
        Vector3 moveVector = Vector3.zero;
        moveVector = cumulativeDelta - previousManipulationPosition;
        previousManipulationPosition = cumulativeDelta;
        
        // disable the y-move as it doesn't make sense to have buildings flying around,
        // and also it makes it easier just to limit to this script for translation (vs placeable.cs)
        transform.position += new Vector3(moveVector.x * TranslationSensitivity, 0, moveVector.z * TranslationSensitivity);
    }

    public void UnregisterForTranslation() {
        OnUnregisterForTranslation.Invoke();
    }
}
