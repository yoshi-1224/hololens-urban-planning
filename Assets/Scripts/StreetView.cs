using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;

/// <summary>
/// attach this to a global object that has no chance of being cloned
/// </summary>
public class StreetView : Singleton<StreetView> {

    public const string COMMAND_STREET_VIEW = "street view";
    public const string COMMAND_EXIT_STREET_VIEW = "exit street view";

    public float UserCameraHeight = 1.5f;
    private GameObject mapClone;
    private GameObject originalMapParentObject;
    private GameObject gazePointObject;
    private bool isInStreetViewMode = false;
    private Vector3 cameraPositionBeforeStreetView;

    public void SetUpStreetView() {
        if (isInStreetViewMode)
            return;
        SaveGazePosition();
        CreateScene();
        isInStreetViewMode = true;
    }

    public void SaveGazePosition() {
        if (originalMapParentObject == null)
            originalMapParentObject = GameObject.Find("MapParent(Clone)"); // Clone as instantiated from script
        Vector3 hitPoint = GazeManager.Instance.HitPosition;
        gazePointObject = new GameObject();
        gazePointObject.transform.position = hitPoint;
        // set this to be the child of map so that when the map is scaled 1-to-1 
        // the relative position will still be saved
        gazePointObject.transform.parent = originalMapParentObject.transform;
        cameraPositionBeforeStreetView = Camera.main.transform.position;
        Camera.main.transform.position = gazePointObject.transform.position + new Vector3(0, UserCameraHeight, 0);
        // not required anymore
        Destroy(gazePointObject);
        gazePointObject = null;
    }

    public void CreateScene() {
        // clone the mapObject
        mapClone = Instantiate(originalMapParentObject);

        // disallow duplicate stuff not required in streetView mode

        foreach(Transform child in mapClone.transform) {
            child.localScale = new Vector3(1, 1, 1);
        }
        // hide the object temporarily
        originalMapParentObject.SetActive(false);
    }

    public void ExitStreetView() {
        if (!isInStreetViewMode)
            return;
        Destroy(mapClone);
        originalMapParentObject.SetActive(true);
        // start from the user placing the map in desired location
        originalMapParentObject.GetComponentInChildren<InteractibleMap>().OnInputClicked(null);
        Camera.main.transform.position = cameraPositionBeforeStreetView;
        isInStreetViewMode = false;
    }

}
