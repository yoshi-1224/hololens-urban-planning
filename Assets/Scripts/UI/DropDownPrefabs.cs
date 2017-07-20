using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using HoloToolkit.Unity;

public class DropDownPrefabs : Singleton<DropDownPrefabs> {

    private GameObject instantiated;

    [Tooltip("Prefabs to instantiate when this button is clicked")]
    [SerializeField]
    private List<GameObject> prefabList;

    private Dropdown dropdown;
    float distanceDownFromParent = -0.25f;

    /// <summary>
    /// object instantiated and ready to be dragged onto the map. Only one should
    /// exist at any point
    /// </summary>
    private GameObject objectReadyToPlace = null;

    protected override void Awake() {
        base.Awake();
        dropdown = GetComponent<Dropdown>();    
    }

    /// <summary>
    /// Unity event handler that is called when the prefab dropdown selected item is changed
    /// </summary>
    public void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;
        index--;
        if (objectReadyToPlace != null)
            Destroy(objectReadyToPlace);
        GameObject prefabTypeToInstantiate = prefabList[index];
        instantiated = Instantiate(prefabTypeToInstantiate);
        instantiated.transform.position = transform.position + new Vector3(0, distanceDownFromParent, 0);
        objectReadyToPlace = instantiated;
    }

        // should also be an event handler
    public void AllowNewObjectCreated() {
        objectReadyToPlace = null;
        dropdown.value = 0; // return to the first unused item
    }

    // should be an event handler
    public void OnToolbarMoveOrDisable() {
        if (objectReadyToPlace != null)
            Destroy(objectReadyToPlace);
        objectReadyToPlace = null;
        dropdown.Hide();
    }

}
