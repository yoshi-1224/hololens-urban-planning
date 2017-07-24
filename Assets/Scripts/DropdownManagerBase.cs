using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownManagerBase<T> : MonoBehaviour where T : DropdownManagerBase<T> {

    protected Dropdown dropdown;
    protected List<string> ItemNamesInDropdown;

    private static T instance;
    public static T Instance {
        get {
            return instance;
        }
    }

    /// <summary>
    /// Returns whether the instance has been initialized or not.
    /// </summary>
    public static bool IsInitialized {
        get {
            return instance != null;
        }
    }

    /// <summary>
    /// Base awake method that sets the singleton's unique instance.
    /// </summary>
    protected virtual void Awake() {
        if (instance != null) {
            Debug.LogErrorFormat("Trying to instantiate a second instance of singleton class {0}", GetType().Name);
            return;
        }

        instance = (T)this;
        dropdown = GetComponentInChildren<Dropdown>();
        ItemNamesInDropdown = new List<string>();
    }

    protected virtual void DropDown_OnItemSelected(int index) {
    }

    public virtual void AddItemsToDropdown(List<string> itemsToAdd) {
        List<Dropdown.OptionData> itemData = new List<Dropdown.OptionData>();
        foreach (string buildingName in itemsToAdd) {
            if (ItemNamesInDropdown.Contains(buildingName))
                continue;
            itemData.Add(new Dropdown.OptionData(buildingName));
            ItemNamesInDropdown.Add(buildingName);
        }

        dropdown.AddOptions(itemData);
        dropdown.RefreshShownValue();
    }

    protected virtual void AddItemToDropdown(string itemName) {

    }

    protected virtual void OnDestroy() {
        if (instance == this) {
            instance = null;
        }

        ItemNamesInDropdown = null;
    }

    public virtual void HideDropdownList() {
        dropdown.Hide();
    }

}
