using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for child classes that manage Unity UI Dropdown lists. Can be made singleton
/// such that it is possible to access the instance from any classes
/// </summary>
public class DropDownManagerBase<T> : MonoBehaviour where T : DropDownManagerBase<T> {

    protected Dropdown dropdown;
    
    /// <summary>
    /// provides mapping of index to the name of the item at that index
    /// </summary>
    protected List<string> ItemNamesInDropdown;

    private static T instance;
    public static T Instance {
        get {
            return instance;
        }
    }

    public static bool IsInitialized {
        get {
            return instance != null;
        }
    }

    protected virtual void Awake() {
        if (instance != null) {
            Debug.LogErrorFormat("Trying to instantiate a second instance of singleton class {0}", GetType().Name);
            return;
        }

        instance = (T)this;
        dropdown = GetComponentInChildren<Dropdown>();
        ItemNamesInDropdown = new List<string>();
    }

    /// <summary>
    /// Event handler for the drop down. Override this method to customize how to respond to item selected events
    /// </summary>
    public virtual void DropDown_OnItemSelected(int index) {
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

    public virtual void AddItemToDropdown(string itemName) {
        if (ItemNamesInDropdown.Contains(itemName))
            return;

        dropdown.options.Add(new Dropdown.OptionData(itemName));
        ItemNamesInDropdown.Add(itemName);
        dropdown.RefreshShownValue();
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

    public virtual void OnItemDeleted(string itemNameDeleted) {
        int indexToDelete = ItemNamesInDropdown.IndexOf(itemNameDeleted);
        ItemNamesInDropdown.RemoveAt(indexToDelete);
        DeleteOptionFromDropDownAndRefresh(indexToDelete + 1); // off by one
    }

    private void DeleteOptionFromDropDownAndRefresh(int indexToDelete) {
        dropdown.options.RemoveAt(indexToDelete);
        dropdown.RefreshShownValue();
    }

}
