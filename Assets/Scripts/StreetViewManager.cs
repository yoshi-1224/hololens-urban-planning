using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;

/// <summary>
/// attach this to a global object as exitView command should be recognized regardless
/// of what the gazed object is.
/// </summary>
public class StreetViewManager : Singleton<StreetViewManager> {

    public const string COMMAND_STREET_VIEW = "street view";
    public const string COMMAND_EXIT_STREET_VIEW = "exit";

    [SerializeField]
    private GameObject cursor;
    public float UserCameraHeightAboveGround = 1.5f;

    [SerializeField]
    private GameObject mapObject;

    /// <summary>
    /// fake object that will be used to save user's gaze position during scaling
    /// </summary>
    private GameObject gazePointObject;

    private bool isInStreetViewMode = false;
    private Vector3 originalScale;

    public void SetUpStreetView() {
        if (isInStreetViewMode)
            return;
        SaveGazePosition();
        CreateScene();
        isInStreetViewMode = true;
        GlobalVoiceCommands.Instance.EnterStreetViewMode();
        switchMapCapabilityForStreetView();

    }

    /// <summary>
    /// saves user's gaze position so that this will be the place where the user
    /// will be placed in street view mode.
    /// </summary>
    public void SaveGazePosition() {
        Vector3 hitPoint = GazeManager.Instance.HitPosition;
        gazePointObject = new GameObject();
        gazePointObject.transform.position = hitPoint;

        // set this to be the child of map so that when the map is scaled 1-to-1 
        // the relative position will still be saved
        gazePointObject.transform.parent = mapObject.transform;
    }

    /// <summary>
    /// sets the map to be the original 1-to-1 scale and translates its position
    /// so that the user will be put in the street-view-like scene.
    /// </summary>
    public void CreateScene() {
        //save the current scaling
        originalScale = mapObject.transform.localScale;
        mapObject.transform.localScale = new Vector3(1, 1, 1); // set 1-1 scale

        //now prepare to move the map with respect to the camera position
        gazePointObject.transform.parent = mapObject.transform.parent;
        mapObject.transform.parent = gazePointObject.transform;
        gazePointObject.transform.position = Camera.main.transform.position - new Vector3(0, UserCameraHeightAboveGround, 0);

        ////then revert the transform relationship
        mapObject.transform.parent = gazePointObject.transform.parent;

        //// gazePointObject not required anymore
        Destroy(gazePointObject);
        gazePointObject = null;
        
        if (cursor != null)
            cursor.SetActive(false);
    }

    /// <summary>
    /// exits the street view and starts off from where the user left off with the map
    /// placement enabled
    /// </summary>
    public void ExitStreetView() {
        if (!isInStreetViewMode)
            return;
        restoreScaleAndPosition();

        if (cursor != null)
            cursor.SetActive(true);
        switchMapCapabilityForNormalMode();
        mapObject.GetComponentInParent<InteractibleMap>().PlacementStart();
        isInStreetViewMode = false;
        GlobalVoiceCommands.Instance.ExitStreetViewMode();
    }

    private void restoreScaleAndPosition() {
        mapObject.transform.localScale = originalScale;
        // translate map's position instantly so that it won't take a long time to return to the user's gaze
        mapObject.transform.position = Camera.main.transform.position;
    }

    private void switchMapCapabilityForStreetView() {
        //mapObject.AddComponent<StreetViewMovement>();
        mapObject.GetComponentInParent<InteractibleMap>().enabled = false;
        GlobalVoiceCommands.Instance.HideTools();
    }

    private void switchMapCapabilityForNormalMode() {
        //StreetViewMovement movement = mapObject.GetComponent<StreetViewMovement>();
        //if (movement != null)
        //    Destroy(movement);
        mapObject.GetComponentInParent<InteractibleMap>().enabled = true;
    }
}
