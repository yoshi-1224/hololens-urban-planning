using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using HoloToolkit.Unity;

public class DropDownPrefabs : Singleton<DropDownPrefabs> {

    private GameObject instantiatedPrefab;

    [Tooltip("Prefabs to instantiate when this button is clicked")]
    [SerializeField]
    private List<GameObject> prefabList;

    /// <summary>
    /// dropdown component for prefabs. This should not be attached to a gameobject
    /// which can be disabled AT THE START.
    /// </summary>
    private Dropdown dropdown;

    float distanceDownFromParent = 0.10f;

    /// <summary>
    /// object instantiated and ready to be dragged onto the map. Only one should
    /// exist at any point
    /// </summary>
    private GameObject objectReadyToPlace = null;
    private bool isAtStart = true;

    protected override void Awake() {
        base.Awake();
        dropdown = GetComponentInChildren<Dropdown>();
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
        instantiatedPrefab = Instantiate(prefabTypeToInstantiate);
        instantiatedPrefab.transform.position = transform.position + new Vector3(0, -distanceDownFromParent, 0);
        objectReadyToPlace = instantiatedPrefab;
        objectReadyToPlace.AddComponent<DeleteOnVoice>().OnBeforeDelete += DropDownPrefabs_OnBeforeDelete;
        
    }

    private void DropDownPrefabs_OnBeforeDelete(DeleteOnVoice component) {
        component.OnBeforeDelete -= DropDownPrefabs_OnBeforeDelete;
        objectReadyToPlace = null;
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
        HideDropdown();
    }

    public void HideDropdown() {
        dropdown.Hide();
    }

}
