using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class ScreenMessageManager : Singleton<ScreenMessageManager> {

    private float distanceFromUser = 1.0f;
    private float horizontalOffsetFromGaze = 0.11f;
    private float verticalOffsetFromGaze = 0.08f;
    private Interpolator interpolator;
    private Transform cameraTransform;
    private TextMesh textMesh;

    private void Start() {
        interpolator = GetComponent<Interpolator>();
        interpolator.PositionPerSecond = 10f;
        textMesh = GetComponent<TextMesh>();
        gameObject.SetActive(false);
    }

    void LateUpdate() {
        cameraTransform = Camera.main.transform;
        Vector3 offsetVector = new Vector3(horizontalOffsetFromGaze, verticalOffsetFromGaze, distanceFromUser);
        offsetVector = cameraTransform.TransformVector(offsetVector);
        interpolator.SetTargetPosition(cameraTransform.position + offsetVector);
        interpolator.SetTargetRotation(Quaternion.LookRotation(cameraTransform.forward));
    }

    public void activateMessage(string textToDisplay) {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        textMesh.text = textToDisplay;
    }

    public void deactivateMessage() {
        gameObject.SetActive(false);
    }
}
