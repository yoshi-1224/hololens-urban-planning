using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using HoloToolkit.Unity.InputModule;
using System;

/// <summary>
/// Enumeration containing the surfaces on which a GameObject
/// can be placed.  For simplicity of this sample, only one
/// surface type is allowed to be selected.
/// </summary>
public enum PlacementSurfaces {
    // Horizontal surface with an upward pointing normal.    
    Horizontal = 1,

    // Vertical surface with a normal facing the user.
    Vertical = 2,
}

/// <summary>
/// The Placeable class implements the logic used to determine if a GameObject
/// can be placed on a target surface. Constraints for placement include:
/// 
/// * No part of the GameObject's box collider impacts with another object in the scene
/// * The object lays flat (within specified tolerances) against the surface
/// * The object would not fall off of the surface if gravity were enabled.
/// 
/// This class also provides the following visualizations.
/// * A transparent cube representing the object's box collider.
/// * Shadow on the target surface indicating whether or not placement is valid.
/// 
/// Credit: Microsoft Hololens Academy SpatialMapping Tutorial
/// Modified by: Yoshiaki Nishimura
/// </summary>
public class InteractibleMap : MonoBehaviour, IInputClickHandler, IFocusable {
    [Tooltip("The base material used to render the bounds asset when placement is allowed.")]
    public Material PlaceableBoundsMaterial = null;

    [Tooltip("The base material used to render the bounds asset when placement is not allowed.")]
    public Material NotPlaceableBoundsMaterial = null;

    [Tooltip("The material used to render the placement shadow when placement it allowed.")]
    public Material PlaceableShadowMaterial = null;

    [Tooltip("The material used to render the placement shadow when placement it not allowed.")]
    public Material NotPlaceableShadowMaterial = null;

    [Tooltip("The type of surface on which the object can be placed.")]
    public PlacementSurfaces PlacementSurface = PlacementSurfaces.Horizontal;

    [Tooltip("The user guide to show when gazed at for some time")]
    public GameObject guidePrefab;
    private GameObject guideObject;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    public float gazeDurationTillGuideDisplay;

    private float gazeStartedTime;
    private bool shouldShowGuide;

    [Tooltip("The child object(s) to hide during placement.")]
    public List<GameObject> ChildrenToHide = new List<GameObject>();

    [Tooltip("The sound to play when the map is placed")]
    public AudioClip PlacementSound;

    [Tooltip("scaling sensitivity when the map is being scaled")]
    public float ScalingSensitivity = 0.0001f;

    private AudioSource audioSource;
    private GameObject scaleIndicator;

    /// <summary>
    /// Indicates if the object is in the process of being placed.
    /// </summary>
    public bool IsPlacing { get; private set; }

    // The most recent distance to the surface.  This is used to 
    // locate the object when the user's gaze does not intersect
    // with the Spatial Mapping mesh.
    private float lastDistance = 2.0f;

    // The distance away from the target surface that the object should hover prior while being placed.
    private float hoverDistance = 0.15f;

    // Threshold (the closer to 0, the stricter the standard) used to determine if a surface is flat.
    private float distanceThreshold = 0.3f;

    // Threshold (the closer to 1, the stricter the standard) used to determine if a surface is vertical.
    private float upNormalThreshold = 0.9f;

    // Maximum distance, from the object, that placement is allowed.
    // This is used when raycasting to see if the object is near a placeable surface.
    private float maximumPlacementDistance = 5.0f;

    // Speed (1.0 being fastest) at which the object settles to the surface upon placement.
    private float placementVelocity = 0.09f;

    // Indicates whether or not this script manages the object's box collider.
    private bool managingBoxCollider = false;

    // The box collider used to determine of the object will fit in the desired location.
    // It is also used to size the bounding cube.
    private BoxCollider boxCollider = null;
    
    // These assets are sized using the box collider's bounds.
    private GameObject boundsAsset = null;
    private GameObject shadowAsset = null;

    // The location at which the object will be placed.
    private Vector3 targetPosition;

    /// <summary>
    /// Used to avoid unnecesary Update() statements once object has been placed successfully
    /// </summary>
    private bool placingComplete;
    private bool isBeingScaled;
    private GameObject surfacePlanes;
    private Material[] defaultMaterials;
    private Renderer mapRenderer;

    private void Awake() {
        targetPosition = gameObject.transform.position;

        // Get the object's collider.
        boxCollider = gameObject.GetComponent<BoxCollider>();
        if (boxCollider == null) {
            managingBoxCollider = true;
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.enabled = false;
        }

        // Create the object that will be used to indicate the bounds of the GameObject.
        boundsAsset = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundsAsset.transform.parent = gameObject.transform;
        boundsAsset.SetActive(false);

        // Create a object that will be used as a shadow.
        shadowAsset = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadowAsset.transform.parent = gameObject.transform;
        shadowAsset.SetActive(false);

        // added by me
        defaultMaterials = GetComponent<Renderer>().materials;
        EnableAudioHapticFeedback();
        mapRenderer = GetComponent<Renderer>();
        shouldShowGuide = true;
        gazeStartedTime = -1;
        isBeingScaled = false;
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        if (isBeingScaled)
            return;
        if (!IsPlacing) {
            MakeSiblingsChildren();
            OnPlacementStart();
        } else {
            OnPlacementStop();
        }
    }

    private void Update() {
        if (IsPlacing) { // being selected by the user
            // Move the object.
            Move();

            // Set the visual elements.
            Vector3 targetPosition;
            Vector3 surfaceNormal;
            bool canBePlaced = ValidatePlacement(out targetPosition, out surfaceNormal);
            DisplayBounds(canBePlaced);
            DisplayShadow(targetPosition, surfaceNormal, canBePlaced);
            
        }  else {

            if (!placingComplete) {
                // enable the renderer when the placement position is confirmed
                // and hide the shadow
                mapRenderer.enabled = true;
                boundsAsset.SetActive(false);
                shadowAsset.SetActive(false);
                // Gracefully place the object on the target surface.
                // Animation-stuff so do not remove this Update loop
                float dist = (gameObject.transform.position - targetPosition).magnitude;
                if (dist > 0) {
                    gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, targetPosition, placementVelocity / dist);
                } else {
                    // transform.position has been confirmed in a new location
                    // we no longer have to perform the above statements so set placingComplete to true
                    // and exit the looping condition
                    for (int i = 0; i < ChildrenToHide.Count; i++) {
                        ChildrenToHide[i].SetActive(true);
                    }
                    placingComplete = true;
                    MakeChildrenSiblings();
                    resetShowStatus();
                }
            } else {
                if (!shouldShowGuide || guideObject != null)
                    // for any gaze session if the user is doing something
                    // or the guideObject already exists then
                    return;

                if (gazeStartedTime != -1) { // the user is currently gazing at this object
                    if (Time.unscaledTime - gazeStartedTime >= gazeDurationTillGuideDisplay) {
                        // the user has been gazing at this object for gazeDurationTillGuideDisplay
                        showGuideObject();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Put the object into placement mode.
    /// </summary>
    public void OnPlacementStart() {
        // If we are managing the collider, enable it. 
        if (managingBoxCollider) {
            boxCollider.enabled = true;
        }
        // Hide the child object(s) to make placement easier.
        for (int i = 0; i < ChildrenToHide.Count; i++) {
            ChildrenToHide[i].SetActive(false);
        }

        // show the planes during placement and hide the map renderer
        SurfaceMeshesToPlanes.Instance.activatePlanes();
        mapRenderer.enabled = false;

        // Tell the gesture manager that it is to assume
        // all input is to be given to this object.
        InputManager.Instance.OverrideFocusedObject = gameObject;

        // Enter placement mode.
        IsPlacing = true;
        placingComplete = false;
        playPlacementAudio();
        shouldShowGuide = false;
        HideGuideObject();
    }

    /// <summary>
    /// Take the object out of placement mode.
    /// </summary>
    /// <remarks>
    /// This method will leave the object in placement mode if called while
    /// the object is in an invalid location.  To determine whether or not
    /// the object has been placed, check the value of the IsPlacing property.
    /// </remarks>
    public void OnPlacementStop() {
        // ValidatePlacement requires a normal as an out parameter.
        Vector3 position;
        Vector3 surfaceNormal;

        // Check to see if we can exit placement mode.
        if (!ValidatePlacement(out position, out surfaceNormal)) {
            return;
        }

        // added by me
        SurfaceMeshesToPlanes.Instance.deactivatePlanes();

        // The object is allowed to be placed.
        // We are placing at a small buffer away from the surface.
        targetPosition = position + (0.01f * surfaceNormal);

        OrientObject(true, surfaceNormal);

        // If we are managing the collider, disable it. 
        if (managingBoxCollider) {
            boxCollider.enabled = false;
        }

        // Tell the gesture manager that it is to resume its normal behavior.
        InputManager.Instance.OverrideFocusedObject = null;

        // Exit placement mode.
        IsPlacing = false;
        playPlacementAudio();
    }

#region positioning-related


    /// <summary>
    /// Verify whether or not the object can be placed.
    /// </summary>
    /// <param name="position">
    /// The target position on the surface.
    /// </param>
    /// <param name="surfaceNormal">
    /// The normal of the surface on which the object is to be placed.
    /// </param>
    /// <returns>
    /// True if the target position is valid for placing the object, otherwise false.
    /// </returns>
    private bool ValidatePlacement(out Vector3 position, out Vector3 surfaceNormal) {
        Vector3 raycastDirection = gameObject.transform.forward;

        if (PlacementSurface == PlacementSurfaces.Horizontal) {
            // Raycast from the bottom face of the box collider.
            raycastDirection = -(Vector3.up); //straight down
        }

        // Initialize out parameters.
        position = Vector3.zero;
        surfaceNormal = Vector3.zero;

        Vector3[] facePoints = GetColliderFacePoints();

        // The origin points we receive are in local space and we 
        // need to raycast in world space.
        for (int i = 0; i < facePoints.Length; i++) {
            facePoints[i] = gameObject.transform.TransformVector(facePoints[i]) + gameObject.transform.position;
        }
        // Cast a ray from the center of the box collider face to the surface.
        RaycastHit centerHit;
        if (!Physics.Raycast(facePoints[0],
                        raycastDirection,
                        out centerHit,
                        maximumPlacementDistance,
                        SpatialMappingManager.Instance.LayerMask)) {
            // If the ray failed to hit the surface, we are done.
            return false;
        }

        // We have found a surface.  Set position and surfaceNormal.
        position = centerHit.point;
        surfaceNormal = centerHit.normal;

        // Cast a ray from the corners of the box collider face to the surface.
        for (int i = 1; i < facePoints.Length; i++) {
            RaycastHit hitInfo;
            if (Physics.Raycast(facePoints[i],
                                raycastDirection,
                                out hitInfo,
                                maximumPlacementDistance,
                                SpatialMappingManager.Instance.LayerMask)) {
                // To be a valid placement location, each of the corners must have a similar
                // enough distance to the surface as the center point
                if (!IsEquivalentDistance(centerHit.distance, hitInfo.distance)) {
                    return false;
                }
            }
            else {
                // The raycast failed to intersect with the target layer.
                return false;
            }
        }

        // checking there is no vertical intersection by the wall
        // method 1) have reference to all the surface planes created, iterate through
        // each of their colliders.bounds and check if any of them intesect with 
        // this collider:
        if (surfacePlanes == null)
            surfacePlanes = GameObject.Find("SurfacePlanes");
        foreach (BoxCollider collider in surfacePlanes.GetComponentsInChildren<BoxCollider>()) {
            if (collider.bounds.Intersects(boxCollider.bounds))
                return false;
        }
        // CONCLUSION: the bounds are NOT accurate so does not work as expected

        //// method 2: using Physics.Checkbox slightly above the map object
        //Vector3 aboveCenter = facePoints[0] + new Vector3(0, 0.5f, 0); // shift the center up by 2m
        //Debug.Log(" above center " + aboveCenter);
        //Vector3 extents = (boxCollider.size / 4);

        //extents.y = 0.00001f; // something really thin
        //Debug.Log(" extents " + extents);
        //if (Physics.CheckBox(aboveCenter, extents)) {
        //    return false;
        //}

        // CONCLUSION: doesn't work anyways.

        return true;
    }

    /// <summary>
    /// Determine the coordinates, in local space, of the box collider face that 
    /// will be placed against the target surface.
    /// </summary>
    /// <returns>
    /// Vector3 array with the center point of the face at index 0.
    /// </returns>
    private Vector3[] GetColliderFacePoints() {
        // Get the collider extents.  
        // The size values are twice the extents.
        Vector3 extents = boxCollider.size / 2;

        // Calculate the min and max values for each coordinate.
        float minX = boxCollider.center.x - extents.x;
        float maxX = boxCollider.center.x + extents.x;
        float minY = boxCollider.center.y - extents.y;
        float maxY = boxCollider.center.y + extents.y;
        float minZ = boxCollider.center.z - extents.z;
        float maxZ = boxCollider.center.z + extents.z;

        Vector3 center;
        Vector3 corner0;
        Vector3 corner1;
        Vector3 corner2;
        Vector3 corner3;

        if (PlacementSurface == PlacementSurfaces.Horizontal) {
            // Placing on horizontal surfaces.
            center = new Vector3(boxCollider.center.x, minY, boxCollider.center.z);
            corner0 = new Vector3(minX, minY, minZ);
            corner1 = new Vector3(minX, minY, maxZ);
            corner2 = new Vector3(maxX, minY, minZ);
            corner3 = new Vector3(maxX, minY, maxZ);
        }
        else {
            // Placing on vertical surfaces.
            center = new Vector3(boxCollider.center.x, boxCollider.center.y, maxZ);
            corner0 = new Vector3(minX, minY, maxZ);
            corner1 = new Vector3(minX, maxY, maxZ);
            corner2 = new Vector3(maxX, minY, maxZ);
            corner3 = new Vector3(maxX, maxY, maxZ);
        }

        return new Vector3[] { center, corner0, corner1, corner2, corner3 };
    }

    /// <summary>
    /// Positions the object along the surface toward which the user is gazing.
    /// </summary>
    /// <remarks>
    /// If the user's gaze does not intersect with a surface, the object
    /// will remain at the most recently calculated distance.
    /// </remarks>
    private void Move() {
        Vector3 moveTo = gameObject.transform.position;
        Vector3 surfaceNormal = Vector3.zero;
        RaycastHit hitInfo;

        bool hit = Physics.Raycast(Camera.main.transform.position,
                                Camera.main.transform.forward,
                                out hitInfo,
                                20f,
                                SpatialMappingManager.Instance.LayerMask);

        if (hit) {
            float offsetDistance = hoverDistance;

            // Place the object a small distance away from the surface while keeping 
            // the object from going behind the user.
            if (hitInfo.distance <= hoverDistance) {
                offsetDistance = 0f;
            }

            moveTo = hitInfo.point + (offsetDistance * hitInfo.normal);

            lastDistance = hitInfo.distance;
            surfaceNormal = hitInfo.normal;
        }
        else {
            // The raycast failed to hit a surface.  In this case, keep the object at the distance of the last
            // intersected surface.
            moveTo = Camera.main.transform.position + (Camera.main.transform.forward * lastDistance);
        }

        // Follow the user's gaze.
        float dist = Mathf.Abs((gameObject.transform.position - moveTo).magnitude);
        gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, moveTo, placementVelocity / dist);

        // Orient the object.
        // We are using the return value from Physics.Raycast to instruct
        // the OrientObject function to align to the vertical surface if appropriate.
        OrientObject(hit, surfaceNormal);
    }

    /// <summary>
    /// Orients the object so that it faces the user.
    /// </summary>
    /// <param name="alignToVerticalSurface">
    /// If true and the object is to be placed on a vertical surface, 
    /// orient parallel to the target surface.  If false, orient the object 
    /// to face the user.
    /// </param>
    /// <param name="surfaceNormal">
    /// The target surface's normal vector.
    /// </param>
    /// <remarks>
    /// The aligntoVerticalSurface parameter is ignored if the object
    /// is to be placed on a horizontalSurface
    /// </remarks>
    private void OrientObject(bool alignToVerticalSurface, Vector3 surfaceNormal) {
        Quaternion rotation = Camera.main.transform.localRotation;

        // If the user's gaze does not intersect with the Spatial Mapping mesh,
        // orient the object towards the user.
        if (alignToVerticalSurface && (PlacementSurface == PlacementSurfaces.Vertical)) {
            // We are placing on a vertical surface.
            // If the normal of the Spatial Mapping mesh indicates that the
            // surface is vertical, orient parallel to the surface.
            if (Mathf.Abs(surfaceNormal.y) <= (1 - upNormalThreshold)) {
                rotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);
            }
        }
        else {
            rotation.x = 0f;
            rotation.z = 0f;
        }

        gameObject.transform.rotation = rotation;
    }

    /// <summary>
    /// Displays the bounds asset.
    /// </summary>
    private void DisplayBounds(bool canBePlaced) {
        // Ensure the bounds asset is sized and positioned correctly.
        boundsAsset.transform.localPosition = boxCollider.center;
        boundsAsset.transform.localScale = boxCollider.size;
        boundsAsset.transform.rotation = gameObject.transform.rotation;

        // Apply the appropriate material.
        if (canBePlaced) {
            boundsAsset.GetComponent<Renderer>().sharedMaterial = PlaceableBoundsMaterial;
        }
        else {
            boundsAsset.GetComponent<Renderer>().sharedMaterial = NotPlaceableBoundsMaterial;
        }

        // Show the bounds asset.
        boundsAsset.SetActive(true);
    }

    /// <summary>
    /// Displays the placement shadow asset.
    /// </summary>
    private void DisplayShadow(Vector3 position, Vector3 surfaceNormal, bool canBePlaced) {
        // Rotate and scale the shadow so that it is displayed on the correct surface and matches the object.
        float rotationX = 0.0f;

        if (PlacementSurface == PlacementSurfaces.Horizontal) {
            rotationX = 90.0f;
            shadowAsset.transform.localScale = new Vector3(boxCollider.size.x, boxCollider.size.z, 1);
        }
        else {
            shadowAsset.transform.localScale = boxCollider.size;
        }

        Quaternion rotation = Quaternion.Euler(rotationX, gameObject.transform.rotation.eulerAngles.y, 0);
        shadowAsset.transform.rotation = rotation;

        // Apply the appropriate material.
        if (canBePlaced) {
            shadowAsset.GetComponent<Renderer>().sharedMaterial = PlaceableShadowMaterial;
        }
        else {
            shadowAsset.GetComponent<Renderer>().sharedMaterial = NotPlaceableShadowMaterial;
        }

        // Show the shadow asset as appropriate.        
        if (position != Vector3.zero) {
            // Position the shadow a small distance from the target surface, along the normal.
            shadowAsset.transform.position = position + (0.01f * surfaceNormal);
            shadowAsset.SetActive(true);
        }
        else {
            shadowAsset.SetActive(false);
        }
    }
    /// <summary>
    /// returns true if the difference in distance is within tolerance
    /// </summary>
    private bool IsEquivalentDistance(float d1, float d2) {
        float dist = Mathf.Abs(d1 - d2);
        return (dist <= distanceThreshold);
    }

#endregion

    /// <summary>
    /// Called when the GameObject is unloaded.
    /// </summary>
    private void OnDestroy() {
        // Unload objects we have created.
        Destroy(boundsAsset);
        boundsAsset = null;
        Destroy(shadowAsset);
        shadowAsset = null;
        HideGuideObject();
    }

#region audio-related
    /// <summary>
    /// sets up the audio feedback on this object. The clip attached will then be able to play
    /// by calling playPlacementAudio()
    /// </summary>
    private void EnableAudioHapticFeedback() {
        if (PlacementSound == null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = PlacementSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.dopplerLevel = 0;
    }

    private void playPlacementAudio() {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

#endregion

    /// <summary>
    /// This should be called right before the placing starts so that the buildings follow
    /// the transform of map
    /// </summary>
    public void MakeSiblingsChildren() {
        foreach(GameObject child in ChildrenToHide) {
            child.transform.parent = transform;
        }
    }

    /// <summary>
    /// This should be called right after the placing ends so that the buildings become 
    /// independent from the map and can receive their own select event handlers
    /// </summary>
    public void MakeChildrenSiblings() {
        foreach(GameObject sibling in ChildrenToHide) {
            sibling.transform.parent = transform.parent;
        }
    }

    public void OnFocusEnter() {
        gazeStartedTime = Time.unscaledTime;
        EnableEmission();
    }

    public void OnFocusExit() {
        gazeStartedTime = -1;
        DisableEmission();
        HideGuideObject();
    }

#region visual feedbacks
    /// <summary>
    /// enable emission so that when this building is focused the material lights up
    /// to give the user visual feedback
    /// </summary>
    public void EnableEmission() {
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].EnableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// disable emission when gaze is exited from this building
    /// </summary>
    public void DisableEmission() {
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].DisableKeyword("_EMISSION");
        }
    }

#endregion

#region guide-related
    private void showGuideObject() {
        if (guideObject == null)
            guideObject = Instantiate(guidePrefab);
        fillGuideDetails();
        positionGuideObject();
    }

    private void fillGuideDetails() {
        TextMesh textMesh = guideObject.GetComponent<TextMesh>();
        textMesh.text =
            "<b>Valid commands:</b>\nScale Map\nMove Map\nStreet View";
        textMesh.fontSize = 52;
        float scale = 0.005f;
        guideObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void HideGuideObject() {
        if (guideObject != null)
            Destroy(guideObject);
        guideObject = null;
    }

    private void positionGuideObject() {
        float distanceRatio = 0.2f;
        guideObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * GazeManager.Instance.HitPosition;
        guideObject.transform.rotation = Quaternion.LookRotation(GazeManager.Instance.HitPosition - Camera.main.transform.position, Vector3.up);
    }

    private void resetShowStatus() {
        shouldShowGuide = true;
        gazeStartedTime = -1;
    }

#endregion

#region scaling-related
    public void PerformScalingStarted() {
        if (!IsPlacing)
            MakeSiblingsChildren();
        isBeingScaled = true;
    }

    public void PerformScalingUpdate(Vector3 cumulativeDelta) {
        float yMovement = cumulativeDelta.y;
        float scalingFactor = yMovement * ScalingSensitivity;
        transform.localScale += new Vector3(scalingFactor, scalingFactor, scalingFactor);
        UpdateMapInfo();
    }

    public void RegisterForScaling() {
        shouldShowGuide = false;
        HideGuideObject();
        GestureManager.Instance.RegisterGameObjectForScaling(gameObject);
    }

    public void UnregisterCallBack() {
        resetShowStatus();
        isBeingScaled = false;
        if (!IsPlacing)
            MakeChildrenSiblings();
    }

#endregion

    /// <summary>
    /// call this whenever the scaling for the map has been changed so that we can 
    /// update the number displayed to the user using mapInfo object
    /// </summary>
    public void UpdateMapInfo() {
        if (scaleIndicator == null)
            scaleIndicator = GameObject.Find("ScaleIndicator");
        scaleIndicator.SendMessage("UpdateCurrentScaling", transform.localScale.x);
    }
}