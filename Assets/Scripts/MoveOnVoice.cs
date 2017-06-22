using HoloToolkit.Unity.InputModule;
using UnityEngine;
using HoloToolkit.Unity;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Interpolator))]
public class MoveOnVoice: MonoBehaviour, IInputClickHandler, IFocusable {

    private float Distance = 2.5f;
    private Transform cameraTransform;
    private Interpolator interpolator;
    private float RotationSensitivity = 10f;
    // used for translation to get the moveVector
    private Vector3 previousManipulationPosition;
    /// <summary>
    /// Keeps track of if the user is moving the object or not.
    /// Setting this to true will enable the user to move and place the object in the scene.
    /// Useful when you want to place an object immediately.
    /// </summary>
    [Tooltip("Setting this to true will enable the user to move and place the object in the scene without needing to tap on the object. Useful when you want to place an object immediately.")]
    public bool IsBeingPlaced;

    /// added
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
    public float ScalingSensitivity = 0.0002f;

    private AudioSource audioSource;
    private GameObject scaleIndicator;

    private Material[] defaultMaterials;

    private void Start() {
        // Make sure we have all the components in the scene we need.
        interpolator = GetComponent<Interpolator>();
        interpolator.PositionPerSecond = 30f;
        defaultMaterials = GetComponent<Renderer>().materials;
        EnableAudioHapticFeedback();
        shouldShowGuide = true;
        gazeStartedTime = -1;
    }

    private void Update() {
        if (IsBeingPlaced) {
            cameraTransform = Camera.main.transform;
            interpolator.SetTargetPosition(cameraTransform.position + (cameraTransform.forward * Distance));
            interpolator.SetTargetRotation(Quaternion.Euler(0, cameraTransform.localEulerAngles.y -180, 0));
        }
    }

    public virtual void OnInputClicked(InputClickedEventData eventData) {
        IsBeingPlaced = !IsBeingPlaced;
        if (IsBeingPlaced) {
            OnPlacementStart();
        } else {
            OnPlacementStop();
        }
    }

    private void OnPlacementStart() {
        playPlacementAudio();
        MakeSiblingsChildren();
        HideChildren();
    }

    private void OnPlacementStop() {
        playPlacementAudio();
        MakeChildrenSiblings();
        ShowChildren();
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

#region transform-related
    /// <summary>
    /// This should be called right before the placing starts so that the buildings follow
    /// the transform of map
    /// </summary>
    public void MakeSiblingsChildren() {
        foreach (GameObject child in ChildrenToHide) {
            child.transform.parent = transform;
        }
    }

    /// <summary>
    /// This should be called right after the placing ends so that the buildings become 
    /// independent from the map and can receive their own select event handlers
    /// </summary>
    public void MakeChildrenSiblings() {
        foreach (GameObject sibling in ChildrenToHide) {
            sibling.transform.parent = transform.parent;
        }
    }

    public void HideChildren() {
        for (int i = 0; i < ChildrenToHide.Count; i++) {
            ChildrenToHide[i].SetActive(false);
        }
    }

    public void ShowChildren() {
        for (int i = 0; i < ChildrenToHide.Count; i++) {
            ChildrenToHide[i].SetActive(true);
        }
    }

#endregion

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
            "<b>Valid commands:</b>\nRotate Map\nScale Map\nMove Map\nStreet View";
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

    private void AllowGuideObject() {
        shouldShowGuide = true;
        gazeStartedTime = -1;
    }
    
    private void DisallowGuideObject() {
        shouldShowGuide = false;
        HideGuideObject();
    }

#endregion

#region scaling-related
    public void PerformScalingStarted(Vector3 cumulativeDelta) {
        if (!IsBeingPlaced)
            MakeSiblingsChildren();
        foreach (Interactible script in GetComponentsInChildren<Interactible>()) {
            script.HideDetails();
        }
    }

    public void PerformScalingUpdate(Vector3 cumulativeDelta) {
        float yMovement = cumulativeDelta.y;
        float scalingFactor = yMovement * ScalingSensitivity;
        float minimumScale = 0.0005f;
        float maximumScale = 0.005f;
        float currentScale = transform.localScale.x;
        bool notifyUser = true;
        if (currentScale + scalingFactor > maximumScale) {
            transform.localScale = new Vector3(maximumScale, maximumScale, maximumScale);
        } else if (currentScale + scalingFactor < minimumScale) {
            transform.localScale = new Vector3(minimumScale, minimumScale, minimumScale);
        } else {
            transform.localScale += new Vector3(scalingFactor, scalingFactor, scalingFactor);
            notifyUser = false;
        }

        UpdateMapInfo(notifyUser);
    }

    public void RegisterForScaling() {
        DisallowGuideObject();

        GestureManager.Instance.RegisterGameObjectForScaling(gameObject);
    }

    /// <summary>
    /// call this whenever the scaling for the map has been changed so that we can 
    /// update the number displayed to the user using mapInfo object
    /// </summary>
    public void UpdateMapInfo(bool isExceedingLimit) {
        if (scaleIndicator == null)
            scaleIndicator = GameObject.Find("ScaleIndicator");
        // send any of its scaling component (x, y or z)
        object[] arguments = { transform.localScale.x, isExceedingLimit };
        scaleIndicator.SendMessage("UpdateCurrentScaling", arguments);
    }

#endregion

#region rotation-related
    public void RegisterForRotation() {
        DisallowGuideObject();
        if (!IsBeingPlaced)
            MakeSiblingsChildren();
        GestureManager.Instance.RegisterGameObjectForRotation(gameObject);
    }

    private void PerformRotationStarted(Vector3 cumulativeDelta) {
        foreach (Interactible script in GetComponentsInChildren<Interactible>()) {
            script.HideDetails();
        }
        previousManipulationPosition = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
    }

    /// <summary>
    /// This message is sent from GestureManager instance
    /// </summary>
    /// <param name="cumulativeDelta"></param>
    public void PerformRotationUpdate(Vector3 cumulativeDelta) {
        Vector3 moveVector = Vector3.zero;
        Vector3 cumulativeDeltaInCameraSpace = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
        moveVector = cumulativeDeltaInCameraSpace - previousManipulationPosition;
        previousManipulationPosition = cumulativeDeltaInCameraSpace;

        float rotationFactor = -moveVector.x * RotationSensitivity* 20; // may be wrong by doing this.
        transform.Rotate(new Vector3(0, rotationFactor, 0));
    }

    #endregion
    /// <summary>
    /// called when this object is done with receiving manipulation events.
    /// </summary>
    public void UnregisterCallBack() {
        AllowGuideObject();
        if (!IsBeingPlaced)
            MakeChildrenSiblings();
    }

}
