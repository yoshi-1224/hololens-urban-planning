using UnityEngine;
using HoloToolkit.Unity;
using UnityEngine.UI;

/// <summary>
/// This class is used to manage the screen message that gets displayed right before the user's field of view.
/// </summary>
[RequireComponent(typeof(Interpolator))]
public class ScreenMessageManager : Singleton<ScreenMessageManager> {

    private float distanceFromUser = 1.0f;
    private float horizontalOffsetFromGaze = 0.1f;
    private float verticalOffsetFromGaze = 0.04f;
    private Interpolator interpolator;
    private Transform cameraTransform;

    [SerializeField]
    private Text textComponent;

    private void Start() {
        interpolator = GetComponent<Interpolator>();
        interpolator.PositionPerSecond = 10f;
        textComponent = GetComponentInChildren<Text>();
        cameraTransform = Camera.main.transform;
        gameObject.SetActive(false); // hide at the start
    }

    void LateUpdate() {
        Vector3 offsetVector = new Vector3(horizontalOffsetFromGaze, verticalOffsetFromGaze, distanceFromUser);
        offsetVector = cameraTransform.TransformVector(offsetVector);
        interpolator.SetTargetPosition(cameraTransform.position + offsetVector);
        interpolator.SetTargetRotation(Quaternion.LookRotation(cameraTransform.forward));
    }

    public void DisplayMessage(string textToDisplay, Color messageColor) {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        textComponent.text = textToDisplay;
        textComponent.color = messageColor;
    }

    public void DeactivateMessage() {
        gameObject.SetActive(false);
    }
}
