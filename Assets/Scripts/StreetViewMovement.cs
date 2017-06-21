using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
/// <summary>
/// attach this to the CustomizedMap gameobject whose transform contains
/// all the buildings so that they move together with the map
/// </summary>
public class StreetViewMovement : MonoBehaviour, IManipulationHandler {

    Vector3 previousManipulationPosition;
    private float horizontalSensitivity = 200f;
    private float verticalSensitivity = 300f;

    private void Start() { 
        InputManager.Instance.PushModalInputHandler(gameObject);
        Debug.Log("Street view movement enabled");
    }

    /// <summary>
    /// This message is sent from GestureManager instance.
    /// </summary>
    public void PerformTranslationStarted(Vector3 cumulativeDelta) {
        previousManipulationPosition = cumulativeDelta;
    }

    public void OnManipulationCanceled(ManipulationEventData eventData) {
    }

    public void OnManipulationCompleted(ManipulationEventData eventData) {
    }

    public void OnManipulationStarted(ManipulationEventData eventData) {
        Vector3 cumulativeDelta = eventData.CumulativeDelta;
        previousManipulationPosition = cumulativeDelta;
    }

    public void OnManipulationUpdated(ManipulationEventData eventData) {
        Vector3 cumulativeDelta = eventData.CumulativeDelta;
        Vector3 moveVector = Vector3.zero;
        moveVector = cumulativeDelta - previousManipulationPosition;
        previousManipulationPosition = cumulativeDelta;

        Vector3 towardsUser = -Camera.main.transform.forward;
        towardsUser.y = 0;
        Vector3 toTheRight = Quaternion.Euler(0, 90, 0) * towardsUser; // quat on left
        Debug.Log("user looking at " + towardsUser);

        // y movement vector sets the z-vector
        transform.position += towardsUser * verticalSensitivity * moveVector.y - toTheRight * horizontalSensitivity * moveVector.x;
    }

    private void OnDestroy() {
        InputManager.Instance.ClearModalInputStack();
        Debug.Log("Street view movement disabled");
    }
}
