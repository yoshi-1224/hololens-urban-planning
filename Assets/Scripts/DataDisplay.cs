using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class DataDisplay : Singleton<DataDisplay> {
    public GameObject MapInfo;
    private bool isAtstart = true;

    private void OnEnable() {
        Debug.Log("Enabled");
        if (!isAtstart)
            UpdateMapInfo();
        isAtstart = false;
    }

    public void UpdateMapInfo() {
        MapInfo = GameObject.Find("MapInfo");
        if (MapInfo != null) // if active
            MapInfo.GetComponent<TextMesh>().text = string.Format("<b>Map Scale</b>: {0:0.0000}", TableDataHolder.Instance.MapScale);
    }
}
