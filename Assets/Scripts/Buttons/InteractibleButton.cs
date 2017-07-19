using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class InteractibleButton : ButtonBase {

    [Tooltip("Prefab to instantiate when this button is clicked")]
    public GameObject prefabToInstantiate;
    private GameObject instantiated;

    /// <summary>
    /// object instantiated and ready to be dragged onto the map. Only one should
    /// exist at any point in time and is why is a static variable
    /// </summary>
    public static GameObject objectReadyToPlace = null;

 //   protected override void Start () {
 //       base.Start();
	//}
    
    public void InstantiatePrefab() {
        if (objectReadyToPlace != null)
            Destroy(objectReadyToPlace);
        instantiated = Instantiate(prefabToInstantiate);
        // set the parent transform of the instantiated prefab to the transform of building collection
        // AFTER it has been placed somewhere
        float distanceDownFromParent = -0.45f;
        GameObject buttonsParent = GameObject.Find("PrefabsButtons");
        instantiated.transform.position = buttonsParent.transform.position + new Vector3(0, distanceDownFromParent, 0);
        objectReadyToPlace = instantiated;
    }

    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        InstantiatePrefab();
    }

    public static void AllowNewObjectCreated() {
        objectReadyToPlace = null;
    }

    public static void onToolbarMoveOrDisable() {
        if (objectReadyToPlace != null)
            Destroy(objectReadyToPlace);
        objectReadyToPlace = null;
    }

}
