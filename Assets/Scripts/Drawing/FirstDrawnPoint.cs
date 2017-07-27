using UnityEngine;
using HoloToolkit.Unity.InputModule;

/// <summary>
/// This component should be attached to the point first drawn by DrawingManager. This class notifies DrawingManager whether the instantiation of a polygon is possible or not when the user gaze focuse son this game object.
/// </summary>
public class FirstDrawnPoint : MonoBehaviour, IFocusable {
    private float scalingFactor = 1.3f;
    private bool isEnlarged;

    private void Start() {
        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.radius *= 3; //make it an easier target to hit

        isEnlarged = false;
    }

    private void enlargeToShowFeedback() {
        if (!isEnlarged)
            transform.localScale *= scalingFactor;
        isEnlarged = true;
    }

    private void returnToNormalScale() {
        if (isEnlarged)
            transform.localScale /= scalingFactor;
        isEnlarged = false;
    }

    public void OnFocusEnter() {
        if (DrawingManager.Instance.CheckCanPolygonBeEnclosed()) {
            DrawingManager.Instance.CanPolygonBeEnclosedAndCursorOnFirstPoint = true;
            enlargeToShowFeedback();
            DrawingManager.Instance.FixLineEndAtFirstSphere();
            DrawingManager.Instance.DisplayGuide(transform.position);
        }
    }

    public void OnFocusExit() {
        DrawingManager.Instance.CanPolygonBeEnclosedAndCursorOnFirstPoint = false;
        returnToNormalScale();
        DrawingManager.Instance.HideGuide();
    }

    private void OnDestroy() {
        if (DrawingManager.Instance != null)
            DrawingManager.Instance.HideGuide();
    }

}
