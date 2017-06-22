using UnityEngine;
using System;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;

/// <summary>
/// Component that allows dragging the building prefab onto the map with hand gesture.
/// On top of the HandDraggable.cs functionality, it has functionalies such as to make the object
/// clip to the map surface.
/// </summary>

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Interactible))]
[RequireComponent(typeof(DeleteOnVoice))]
public class DraggableOntoMap : MonoBehaviour, IFocusable, IInputHandler, ISourceStateHandler {

    public event Action StartedDragging;
    public event Action StoppedDragging;

    [Tooltip("The base material used to render the bounds asset when placement is allowed.")]
    public Material PlaceableBoundsMaterial = null;

    [Tooltip("The base material used to render the bounds asset when placement is not allowed.")]
    public Material NotPlaceableBoundsMaterial = null;

    [Tooltip("Scale by which hand movement in z is multipled to move the dragged object.")]
    public float DistanceScale = 2f;

    private GameObject mapObject;
    private Vector3 originalScale;
    private BoxCollider boxCollider;
    private float currentMapHeight;
    private float heightAboveMapForBottomClipping = 0.1f;
    private bool canBePlaced;

    public enum RotationModeEnum {
        Default,
        LockObjectRotation,
        OrientTowardUser,
        OrientTowardUserAndKeepUpright
    }

    public RotationModeEnum RotationMode = RotationModeEnum.Default;

    [Tooltip("Controls the speed at which the object will interpolate toward the desired position")]
    [Range(0.01f, 1.0f)]
    public float PositionLerpSpeed = 0.2f;

    [Tooltip("Controls the speed at which the object will interpolate toward the desired rotation")]
    [Range(0.01f, 1.0f)]
    public float RotationLerpSpeed = 0.2f;

    private Camera mainCamera;
    private bool isDragging;
    private bool isGazed;
    private Vector3 objRefForward;
    private Vector3 objRefUp;
    private float objRefDistance;
    private Quaternion gazeAngularOffset;
    private float handRefDistance;
    private Vector3 objRefGrabPoint;

    private Vector3 draggingPosition;
    private Quaternion draggingRotation;

    private IInputSource currentInputSource = null;
    private uint currentInputSourceId;

    private Vector3 originalPosition;
    public GameObject boundsAsset;
    private int layerToAvoidRaycast;
    private const int OVER_MAP = 1, ON_MAP = 2, NOT_OVER_MAP = 3;

    private void Start() {
        originalPosition = transform.localPosition;
        mainCamera = Camera.main;
        mapObject = GameObject.Find("CustomizedMap");
        boxCollider = GetComponentInChildren<BoxCollider>(); // children and this object

        initializeBoundsObject();

        layerToAvoidRaycast = LayerMask.NameToLayer("ObjectToPlace");
        MapPlacement.SetLayerRecursively(gameObject, layerToAvoidRaycast);
    }

    private void initializeBoundsObject() {
        boundsAsset = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundsAsset.transform.parent = gameObject.transform;
        boundsAsset.SetActive(false);
    }

    private void OnDestroy() {
        if (isDragging) {
            StopDragging();
        }

        if (isGazed) {
            OnFocusExit();
        }
    }

    private void Update() {
        if (isDragging) {
            UpdateDragging();
        }
    }

    public void StartDragging() {
        if (isDragging) {
            return;
        }

        UpdateTransformValues();
        // Add self as a modal input handler, to get all inputs during the manipulation
        InputManager.Instance.PushModalInputHandler(gameObject);

        isDragging = true;
        GuideStatus.ShouldShowGuide = false;

        Vector3 gazeHitPosition = GazeManager.Instance.HitInfo.point;
        Vector3 handPosition;
        currentInputSource.TryGetPosition(currentInputSourceId, out handPosition);

        Vector3 pivotPosition = GetHandPivotPosition();
        handRefDistance = Vector3.Magnitude(handPosition - pivotPosition);
        objRefDistance = Vector3.Magnitude(gazeHitPosition - pivotPosition);

        Vector3 objForward = transform.forward;
        Vector3 objUp = transform.up;

        // Store where the object was grabbed from
        objRefGrabPoint = mainCamera.transform.InverseTransformDirection(transform.position - gazeHitPosition);

        Vector3 objDirection = Vector3.Normalize(gazeHitPosition - pivotPosition);
        Vector3 handDirection = Vector3.Normalize(handPosition - pivotPosition);

        objForward = mainCamera.transform.InverseTransformDirection(objForward);       // in camera space
        objUp = mainCamera.transform.InverseTransformDirection(objUp);       		   // in camera space
        objDirection = mainCamera.transform.InverseTransformDirection(objDirection);   // in camera space
        handDirection = mainCamera.transform.InverseTransformDirection(handDirection); // in camera space

        objRefForward = objForward;
        objRefUp = objUp;

        // Store the initial offset between the hand and the object, so that we can consider it when dragging
        gazeAngularOffset = Quaternion.FromToRotation(handDirection, objDirection);
        draggingPosition = gazeHitPosition;

        StartedDragging.RaiseEvent();
    }

    public void UpdateTransformValues() {
        originalScale = transform.localScale;
        transform.localScale = mapObject.transform.localScale;
        currentMapHeight = mapObject.transform.position.y;
    }

    public void revertToOriginalScale() {
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Gets the pivot position for the hand, which is approximated to the base of the neck.
    /// </summary>
    /// <returns>Pivot position for the hand.</returns>
    private Vector3 GetHandPivotPosition() {
        Vector3 pivot = Camera.main.transform.position + new Vector3(0, -0.2f, 0) - Camera.main.transform.forward * 0.2f; // a bit lower and behind
        return pivot;
    }

    /// <summary>
    /// Update the position of the object being dragged.
    /// </summary>
    private void UpdateDragging() {
        Vector3 newHandPosition;
        currentInputSource.TryGetPosition(currentInputSourceId, out newHandPosition);

        Vector3 pivotPosition = GetHandPivotPosition();

        Vector3 newHandDirection = Vector3.Normalize(newHandPosition - pivotPosition);

        newHandDirection = mainCamera.transform.InverseTransformDirection(newHandDirection); // in camera space
        Vector3 targetDirection = Vector3.Normalize(gazeAngularOffset * newHandDirection);
        targetDirection = mainCamera.transform.TransformDirection(targetDirection); // back to world space

        float currenthandDistance = Vector3.Magnitude(newHandPosition - pivotPosition);

        float distanceRatio = currenthandDistance / handRefDistance;
        float distanceOffset = distanceRatio > 0 ? (distanceRatio - 1f) * DistanceScale : 0;
        float targetDistance = objRefDistance + distanceOffset;

        draggingPosition = pivotPosition + (targetDirection * targetDistance);

        if (RotationMode == RotationModeEnum.OrientTowardUser || RotationMode == RotationModeEnum.OrientTowardUserAndKeepUpright) {
            draggingRotation = Quaternion.LookRotation(transform.position - pivotPosition);
        }
        else if (RotationMode == RotationModeEnum.LockObjectRotation) {
            draggingRotation = transform.rotation;
        }
        else // RotationModeEnum.Default
        {
            Vector3 objForward = mainCamera.transform.TransformDirection(objRefForward); // in world space
            Vector3 objUp = mainCamera.transform.TransformDirection(objRefUp);   // in world space
            draggingRotation = Quaternion.LookRotation(objForward, objUp);
        }

        // Apply Final Position
        transform.position = Vector3.Lerp(transform.position, draggingPosition + mainCamera.transform.TransformDirection(objRefGrabPoint), PositionLerpSpeed);
        // Apply Final Rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, draggingRotation, RotationLerpSpeed);

        if (RotationMode == RotationModeEnum.OrientTowardUserAndKeepUpright) {
            Quaternion upRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
            transform.rotation = upRotation * transform.rotation;
        }

        int state = getObjectPositionState();
        bool isHeightAdjusted = false;
        // overwrite height and show bounds
        // do not allow the object to be placed below the map and
        //clip to the bottom if necessary
        if (state == ON_MAP || state == OVER_MAP)
            isHeightAdjusted = clipToMapSurfaceIfNeeded();

        canBePlaced = isHeightAdjusted && (state == ON_MAP);
        DisplayBounds(canBePlaced);
    }

    private bool clipToMapSurfaceIfNeeded() {
        Vector3 bottomCentre = GetColliderBottomPointsInWorldSpace()[0];
        float heightGap = bottomCentre.y - currentMapHeight;
        if (heightGap < 0 || heightGap < heightAboveMapForBottomClipping) {
            // want to clip it to the map surface
            Vector3 tempVector = transform.position;
            tempVector.y = currentMapHeight + transform.position.y - bottomCentre.y;
            transform.position = tempVector;
            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Stops dragging the object.
    /// </summary>
    public void StopDragging() {
        if (!isDragging)
            return;

        // Remove self as a modal input handler
        InputManager.Instance.PopModalInputHandler();
        GuideStatus.ShouldShowGuide = true;
        isDragging = false;
        currentInputSource = null;
        StoppedDragging.RaiseEvent();

        if (canBePlaced) {
            transform.parent = GameObject.Find("LOD2").transform;
            // set this so that the bottom surface of this object would be directly on the map
            positionBottomOnTheMap();
            gameObject.GetComponent<Interactible>().enabled = true;
            gameObject.GetComponent<DeleteOnVoice>().enabled = true;
            MapPlacement.SetLayerRecursively(gameObject, LayerMask.NameToLayer("Default"));
            // remove this component
            Destroy(this);
            InteractibleButton.AllowNewObjectCreated();
        } else {
            transform.localPosition = originalPosition;
            revertToOriginalScale();
        }

        boundsAsset.SetActive(false);
    }

    /// <summary>
    /// call this AFTER the object transform has become child of map transform
    /// </summary>
    private void positionBottomOnTheMap() {
        Vector3 tempPosition = transform.localPosition;
        float halfHeight = GetComponentInChildren<MeshFilter>().mesh.bounds.extents.y;
        tempPosition.y = halfHeight;
        transform.localPosition = tempPosition;
    }

    public void OnFocusEnter() {
        isGazed = true;
    }

    public void OnFocusExit() {
        isGazed = false;
    }

    public void OnInputUp(InputEventData eventData) {
        if (currentInputSource != null &&
            eventData.SourceId == currentInputSourceId) {
            StopDragging();
        }
    }

    public void OnInputDown(InputEventData eventData) {
        if (isDragging)
            return;

        if (!eventData.InputSource.SupportsInputInfo(eventData.SourceId, SupportedInputInfo.Position)) {
            // The input source must provide positional data for this script to be usable
            return;
        }

        currentInputSource = eventData.InputSource;
        currentInputSourceId = eventData.SourceId;
        StartDragging();
    }

    public void OnSourceDetected(SourceStateEventData eventData) {
        // Nothing to do
    }

    public void OnSourceLost(SourceStateEventData eventData) {
        if (currentInputSource != null && eventData.SourceId == currentInputSourceId) {
            StopDragging();
        }
    }

    /// <summary>
    /// used to determine if this gameObject is anywhere near the map range. If so,
    /// the transform can be set to be the children of map transform etc.
    /// </summary>
    private int getObjectPositionState() {
        int layerMask = 1 << layerToAvoidRaycast;
        int state = ON_MAP;
        layerMask = ~layerMask;
        RaycastHit hitInfo;
        float maxDistance = 4f;
        float rayCastYPosition = maxDistance / 2;
        Vector3 raycastDirection = Vector3.down;
        Vector3[] facePoints = GetColliderBottomPointsInWorldSpace();
        for (int i = 0; i < facePoints.Length; i++) {
            facePoints[i].y += rayCastYPosition; // make it cast from 2m above
            if (Physics.Raycast(facePoints[i], raycastDirection, out hitInfo, maxDistance, layerMask)) {
                GameObject hitObject = hitInfo.collider.gameObject;
                if (hitObject == mapObject)
                    continue;
                else {
                    return OVER_MAP;
                }
            }
            // if it reaches here, then neither raycat hit nor not the mapObject for this point
            state = NOT_OVER_MAP;
        }

        return state;
    }

    /// <summary>
    /// Visualizes the box collider as well as the condition as to whether the current object
    /// is placeable on the map or not.
    /// </summary>
    private void DisplayBounds(bool canBePlaced) {
        // Ensure the bounds asset is sized and positioned correctly.
        // this is customized to adjust for the wrong pivot point and the parent object.
        boundsAsset.transform.localPosition = boxCollider.center + gameObject.transform.GetChild(0).localPosition;
        boundsAsset.transform.localScale = boxCollider.size;
        boundsAsset.transform.rotation = gameObject.transform.GetChild(0).rotation;

        if (canBePlaced)
            boundsAsset.GetComponent<Renderer>().sharedMaterial = PlaceableBoundsMaterial;
        else
            boundsAsset.GetComponent<Renderer>().sharedMaterial = NotPlaceableBoundsMaterial;

        boundsAsset.SetActive(true);
    }

    private Vector3[] GetColliderBottomPointsInWorldSpace() {
        // Get the collider extents.  
        // The size values are twice the extents.
        Vector3 extents = boxCollider.size / 2;

        // Calculate the min and max values for each coordinate.
        float minX = boxCollider.center.x - extents.x;
        float maxX = boxCollider.center.x + extents.x;
        float minY = boxCollider.center.y - extents.y;
        float minZ = boxCollider.center.z - extents.z;
        float maxZ = boxCollider.center.z + extents.z;

        Vector3 center = new Vector3(boxCollider.center.x, minY, boxCollider.center.z);
        Vector3 corner0 = new Vector3(minX, minY, minZ);
        Vector3 corner1 = new Vector3(minX, minY, maxZ);
        Vector3 corner2 = new Vector3(maxX, minY, minZ);
        Vector3 corner3 = new Vector3(maxX, minY, maxZ);

        Vector3[] facePoints = { center, corner0, corner1, corner2, corner3 };

        for (int i = 0; i < facePoints.Length; i++) {
            facePoints[i] = gameObject.transform.TransformVector(facePoints[i]) + gameObject.transform.position;
        }
        return facePoints;
    }
}
