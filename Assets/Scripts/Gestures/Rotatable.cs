using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Rotatable : MonoBehaviour {
    [SerializeField]
    private float RotationSensitivity = 10f;

    public const string COMMAND_ROTATE = "rotate";
    public event Action OnRegisteringForRotation = delegate { };
    public event Action OnUnregisterForRotation = delegate { };
    public event Action OnRotationUpdated = delegate { };

    public void RegisterForRotation() {
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
}
