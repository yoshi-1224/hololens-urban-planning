using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapInfo : MonoBehaviour {
    private float currentScaling;
    private TextMesh textMeshCache;
    void Start () {
        // make the map object call this class's updateCurrentScaling with its scale
        GameObject.Find("CustomizedMap").SendMessage("UpdateMapInfo", false);
        textMeshCache = GetComponent<TextMesh>();
    }
	
    /// <summary>
    /// shows the current scaling for the user. arguments array MUST BE an array
    /// with first element being float value and second element boolean
    /// </summary>
    /// <param name="arguments"></param>
    public void UpdateCurrentScaling(object[] arguments) {
        currentScaling = (float) arguments[0];
        if (textMeshCache == null)
            textMeshCache = GetComponent<TextMesh>();
        textMeshCache.text = string.Format("Current Scale: {0:0.0000}", currentScaling);
        bool isExceedingLimit = (bool) arguments[1];
        if (isExceedingLimit) {
            textMeshCache.color = Color.red;
        } else {
            textMeshCache.color = Color.white;
        }
        // TODO: make it more efficient in the future and check for inactive state
        GameObject mapInfoPanel = GameObject.Find("MapInfo");
        if (mapInfoPanel != null)
            mapInfoPanel.GetComponent<TextMesh>().text = string.Format("<b>Map Scale</b>: {0:0.0000}", currentScaling);
    }
}
