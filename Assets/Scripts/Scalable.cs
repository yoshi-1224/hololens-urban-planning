using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class Scalable : MonoBehaviour, ISpeechHandler {
    public const string COMMAND_SCALE = "scale";
    private float ScalingSensitivity = 40f;
    private Transform parent;
    private float minimumHeight = 0.1f;
    private Vector3 previousManipulationPosition;
    private GameObject cursor;
    private static float heightOfAStorey = 5;


    /// <summary>
    /// should be passed when this component is added to this gameObject
    /// </summary>
    public Dictionary<int, int> neighbouringVertexMapping {
        get; set;
    }

    public float Area {
        get {
            return computeBaseArea();
        }
    }

    private Mesh mesh;

    private void Start() {
        mesh = GetComponent<MeshFilter>().mesh;
        Debug.Log("Real world area " + Area / TableDataHolder.Instance.MapScale);
        Debug.Log("Hologram area " + Area);
        
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_SCALE:
                RegisterForScaling();
                break;
            default:
                break;
        }
    }

    #region scaling-related
    public void PerformScalingStarted(Vector3 cumulativeDelta) {
        parent = transform.parent;
        transform.parent = parent.transform.parent;
        previousManipulationPosition = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
    }

    public void PerformScalingUpdate(Vector3 cumulativeDelta) {
        // we should make this one use manipulation gesture rather than navigation
        Vector3 moveVector = Vector3.zero;
        Vector3 cumulativeDeltaInCameraSpace = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
        moveVector = cumulativeDeltaInCameraSpace - previousManipulationPosition;
        previousManipulationPosition = cumulativeDeltaInCameraSpace;
        float currentYScale = transform.localScale.y;
        float scalingFactor = moveVector.y * ScalingSensitivity;

        bool isExceedingLimit;
        if (currentYScale + scalingFactor <= minimumHeight) {
            isExceedingLimit = true;
        }
        else {
            transform.localScale += new Vector3(0, scalingFactor, 0);
            isExceedingLimit = false;
        }

        // notify the user of height, GFA?, number of storeys
        NotifyHeightInfo(isExceedingLimit);
    }

    public void RegisterForScaling() {
        GestureManager.Instance.RegisterGameObjectForScalingUsingManipulation(gameObject);
    }

    public void UnregisterCallBack() {
        transform.parent = parent;
    }

    public void NotifyHeightInfo(bool isExceedingLimit) {
        if (cursor == null)
            cursor = GameObject.Find("CustomCursorWithFeedback");

        mesh.RecalculateBounds();
        float hologramHeight = transform.TransformVector(mesh.bounds.size).y;
        float realWorldHeight = hologramHeight / TableDataHolder.Instance.MapScale;
        int numOfStoreys = estimateNumOfStoreys(realWorldHeight);
        object[] arguments = { realWorldHeight, numOfStoreys, isExceedingLimit };
        cursor.SendMessage("UpdateCurrentHeightInfo", arguments);
    }

    #endregion
    /// <summary>
    /// calculates the LOCAL base area of the polygon using shoelace method
    /// The value that this method returns should be scaled with map scale
    /// to convert into real-world area
    /// </summary>
    public float computeBaseArea() {
        if (neighbouringVertexMapping == null)
            return -1;

        float area = 0;
        // assumption is that the first half of the vertices of mesh represent
        // the top vertices and the second half the bottom vertices
        // we are going to use the top vertices since these are the ones we
        // have neighbouring vertex mapping for (shoelace method requires that
        // the vertices are ordered clockwise/anti-clockwise
        int lastTopVertexIndex = mesh.vertices.Length / 2;

        Vector2[] verticesIn2d = vector3Tovector2InWorldSpace(mesh.vertices);
        for (int i = 0; i < lastTopVertexIndex; i++) {
            int neighbourIndex = neighbouringVertexMapping[i];
            Vector2 thisVector = verticesIn2d[i];
            Vector2 neighbourVector = verticesIn2d[neighbourIndex];
            area += thisVector.x * neighbourVector.y;
            area -= thisVector.y * neighbourVector.x;
        }

        return Math.Abs(area) / 2f;
    }

    private int estimateNumOfStoreys(float buildingHeight) {
        return (int) (buildingHeight / heightOfAStorey) + 1;
    }

    /// <summary>
    /// At the moment, GPR == numOfStoreys since GFA per floor == base area
    /// </summary>
    private float estimateGFA(int numOfStoreys, float siteArea) {
        return numOfStoreys * siteArea;
    }

    private Vector2[] vector3Tovector2InWorldSpace(Vector3[] vector3s) {
        Vector2[] vector2s = new Vector2[vector3s.Length];
        for (int i = 0; i < vector3s.Length; i++) {
            Vector3 vector = transform.TransformPoint(vector3s[i]);
            vector2s[i] = new Vector2(vector.x, vector.z);
        }
        return vector2s;
    }
}
