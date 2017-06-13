using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapInfo : MonoBehaviour {
    private float currentScaling;
    private TextMesh textCache;
    void Start () {
        // make the map object call this class's updateCurrentScaling with its scale
        GameObject.Find("CustomizedMap").SendMessage("UpdateMapInfo");
        textCache = GetComponent<TextMesh>();
    }
	
    public void UpdateCurrentScaling(float mapScale) {
        currentScaling = mapScale;
        if (textCache == null)
            textCache = GetComponent<TextMesh>();
        textCache.text = string.Format("currentScale: {0:0.00000}", currentScaling);

    }
}
