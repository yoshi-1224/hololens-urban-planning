using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using Mapbox.Utils;

public class PlaceOnMap : MonoBehaviour {
    [Tooltip("The base material used to render the bounds asset when placement is allowed.")]
    [SerializeField]
    private Material PlaceableBoundsMaterial = null;

    [Tooltip("The base material used to render the bounds asset when placement is not allowed.")]
    [SerializeField]
    private Material NotPlaceableBoundsMaterial = null;

    private HandDraggable handDraggableComponent;
    private GameObject mapObject;
    private Vector3 originalScale;
    private BoxCollider boxCollider;
    private float currentMapHeight;
    private float heightAboveMapForBottomClipping = 0.1f;
    private bool canBePlaced;

    private Vector3 originalPosition;
    private GameObject boundsAsset;
    private int layerToAvoidRaycast;
    private const int OVER_MAP = 1, ON_MAP = 2, NOT_OVER_MAP = 3;

    void Start () {
        handDraggableComponent = gameObject.AddComponent<HandDraggable>();
        handDraggableComponent.OnDraggingUpdate += HandDraggableComponent_OnDraggingUpdate;
        handDraggableComponent.StartedDragging += HandDraggableComponent_StartedDragging;
        handDraggableComponent.StoppedDragging += HandDraggableComponent_StoppedDragging;

        handDraggableComponent.RotationMode = HandDraggable.RotationModeEnum.OrientTowardUserAndKeepUpright;

        originalPosition = transform.localPosition;
        mapObject = GameObject.Find(GameObjectNamesHolder.NAME_MAP_COLLIDER);
        boxCollider = GetComponentInChildren<BoxCollider>(); // children and this object

        initializeBoundsObject();

        layerToAvoidRaycast = GameObjectNamesHolder.LAYER_OBJECT_BEING_PLACED;
        HoloToolkit.Unity.Utils.SetLayerRecursively(gameObject, layerToAvoidRaycast);
    }

    private void OnDestroy() {
        handDraggableComponent.OnDraggingUpdate -= HandDraggableComponent_OnDraggingUpdate;
        handDraggableComponent.StartedDragging -= HandDraggableComponent_StartedDragging;
        handDraggableComponent.StoppedDragging -= HandDraggableComponent_StoppedDragging;
        Destroy(handDraggableComponent);
    }

    private void initializeBoundsObject() {
        boundsAsset = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundsAsset.transform.parent = gameObject.transform;
        boundsAsset.SetActive(false);
    }

    private void HandDraggableComponent_StoppedDragging() {
        GuideStatus.ShouldShowGuide = true;
        if (canBePlaced) {
            Vector2d coordinates = LocationHelper.WorldPositionToGeoCoordinate(gameObject.transform.position);
            GameObject parentTile = LocationHelper.FindParentTile(coordinates);
            transform.SetParent(parentTile.transform, true);
            // set this so that the bottom surface of this object would be directly on the map
            positionBottomOnTheMap();
            gameObject.GetComponent<InteractibleBuilding>().enabled = true;
            gameObject.GetComponent<DeleteOnVoice>().enabled = true;
            HoloToolkit.Unity.Utils.SetLayerRecursively(gameObject, parentTile.layer);
            // remove this component
            Destroy(this);
            DropDownPrefabs.Instance.AllowNewObjectCreated();
        }
        else {
            transform.localPosition = originalPosition;
            revertToOriginalScale();
        }

        boundsAsset.SetActive(false);

    }

    private void HandDraggableComponent_StartedDragging() {
        GuideStatus.ShouldShowGuide = false;
        UpdateTransformValues();
    }

    private void HandDraggableComponent_OnDraggingUpdate() {
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

    /// <summary>
    /// returns true if clipped, else false
    /// </summary>
    private bool clipToMapSurfaceIfNeeded() {
        Vector3 bottomCentre = GetColliderBottomPointsInWorldSpace()[0];
        float heightGap = bottomCentre.y - currentMapHeight;
        if (heightGap < 0 || heightGap < heightAboveMapForBottomClipping) {
            // want to clip it to the map surface
            Vector3 tempVector = transform.position;
            tempVector.y = currentMapHeight + transform.position.y - bottomCentre.y;
            transform.position = tempVector;
            return true;
        }
        else {
            return false;
        }
    }

    public void UpdateTransformValues() {
        originalScale = transform.localScale;
        transform.localScale = Vector3.one * MapDataDisplay.Instance.MapWorldRelativeScale;
        currentMapHeight = mapObject.transform.position.y;
    }

    public void revertToOriginalScale() {
        transform.localScale = originalScale;
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
