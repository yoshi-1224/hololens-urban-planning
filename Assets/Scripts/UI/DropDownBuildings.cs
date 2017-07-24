//using System.Collections;
//using System;
//using System.Collections.Generic;
//using UnityEngine.UI;
//using HoloToolkit.Unity;

//public class DropDownBuildings : Singleton<DropDownBuildings> {

//    /// <summary>
//    /// dropdown component for buildings. This should not be attached to a gameobject
//    /// which can be disabled AT THE START.
//    /// </summary>
//    private Dropdown dropdown;
//    private List<string> BuildingNamesInDropDown;

//    protected override void Awake() {
//        base.Awake();
//        dropdown = GetComponentInChildren<Dropdown>();
//        BuildingNamesInDropDown = new List<string>();
//    }

//    /// <summary>
//    /// Unity event handler that is called when the prefab dropdown selected item is changed
//    /// </summary>
//    public void DropDown_OnItemSelected(int index) {
//        if (index == 0)
//            return;

//        index--; // as we have a dummy item at index = 0
//        string selectedBuildingName = BuildingNamesInDropDown[index];
//        BuildingManager.CoordinateBoundObject buildingObject;

//        if (BuildingManager.Instance.BuildingsInScene.TryGetValue(selectedBuildingName, out buildingObject)) {
//            if (buildingObject.gameObject == null) 
//                // this should not happen as the list is updated by BuildingManager
//                return;

//            // whether the building gameObject is on any of the active tiles does not matter
//            // as we call LoadNewTiles() anyways
//            LocationHelper.MoveMapToCoordinateAsCenter(buildingObject.coordinates);
//        }

//        dropdown.value = 0; // go back to the dummy value at the top of the list
//    }

//    /// <summary>
//    /// adds the building name to the dropdown menu. The index in dropdown should correspond
//    /// with the index in the list
//    /// </summary>
//    public void AddBuildingsToDropDown(List<string> buildingNamesList) {
//        List<Dropdown.OptionData> buildingData = new List<Dropdown.OptionData>();
//        foreach(string buildingName in buildingNamesList) {
//            if (BuildingNamesInDropDown.Contains(buildingName))
//                continue;
//            buildingData.Add(new Dropdown.OptionData(buildingName));
//            BuildingNamesInDropDown.Add(buildingName);
//        }

//        /// sorting by name might be useful in the future
//        dropdown.AddOptions(buildingData);
//        dropdown.RefreshShownValue();
//    }

//    /// <summary>
//    /// should be called when the toolbar moves
//    /// </summary>
//    public void HideDropdownList() {
//        dropdown.Hide();
//    }

//}

public class DropDownBuildings : DropdownManagerBase<DropDownBuildings> {

    /// <summary>
    /// Unity event handler that is called when the prefab dropdown selected item is changed
    /// </summary>
    protected override void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;

        index--; // as we have a dummy item at index = 0
        string selectedBuildingName = ItemNamesInDropdown[index];
        BuildingManager.CoordinateBoundObject buildingObject;

        if (BuildingManager.Instance.BuildingsInScene.TryGetValue(selectedBuildingName, out buildingObject)) {
            if (buildingObject.gameObject == null)
                // this should not happen as the list is updated by BuildingManager
                return;

            // whether the building gameObject is on any of the active tiles does not matter
            // as we call LoadNewTiles() anyways
            LocationHelper.MoveMapToCoordinateAsCenter(buildingObject.coordinates);
        }

        dropdown.value = 0; // go back to the dummy value at the top of the list
    }

}

