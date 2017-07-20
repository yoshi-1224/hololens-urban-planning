using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using Mapbox.Utils;
using Mapbox.Map;

public class DropDownBuildings : Singleton<DropDownBuildings> {

    /// <summary>
    /// dropdown component attached to this gameObject
    /// </summary>
    private Dropdown dropdown;

    private List<string> BuildingNamesInDropDown;

    protected override void Awake() {
        base.Awake();
        dropdown = GetComponent<Dropdown>();
        BuildingNamesInDropDown = new List<string>();
    }

    /// <summary>
    /// Unity event handler that is called when the prefab dropdown selected item is changed
    /// </summary>
    public void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;

        index--; // as we have a dummy item at index = 0
        InteractibleMap.Instance.HideAllTables();
        string selectedBuildingName = BuildingNamesInDropDown[index];
        GameObject buildingObject = BuildingManager.Instance.BuildingsInScene[selectedBuildingName];

        LocationHelper.MoveMapToWorldPositionAsCenter(buildingObject.transform.position);
        buildingObject.SendMessage("ShowDetails");
    }

    /// <summary>
    /// adds the building name to the dropdown menu. The index in dropdown should correspond
    /// with the index in the list
    /// </summary>
    public void AddBuildingsToDropDown(List<string> buildingNamesList) {
        List<Dropdown.OptionData> buildingData = new List<Dropdown.OptionData>();
        foreach(string buildingName in buildingNamesList) {
            buildingData.Add(new Dropdown.OptionData(buildingName));
            BuildingNamesInDropDown.Add(buildingName);
        }

        /// sorting by name might be useful in the future
        dropdown.AddOptions(buildingData);
        dropdown.RefreshShownValue();
    }

    /// <summary>
    /// should be called when the toolbar moves
    /// </summary>
    public void HideDropdownList() {
        dropdown.Hide();
    }

}
