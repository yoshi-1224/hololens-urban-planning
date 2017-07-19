using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using Mapbox.Utils;

public class UserGeneratedPolygon : MonoBehaviour, ISpeechHandler, IInputClickHandler {
    public const string COMMAND_SCALE = "scale";
    private Transform parent;
    private float minimumHeight = 0.1f;
    private Vector3 previousManipulationPosition;
    private CustomObjectCursor cursor;
    private static float heightOfAStorey = 5;
    private Scalable scalableScript;

    /// <summary>
    /// should be passed when this component is added to this gameObject
    /// </summary>
    public Dictionary<int, int> neighbouringVertexMapping {
        get; set;
    }

    private Vector2d Coordinates;
    public float RealWorldArea {
        get {
            return computeBaseArea() / MapDataDisplay.Instance.MapWorldRelativeScale;
        }
    }
    public float RealWorldHeight {
        get {
            return computeRealWorldHeight();
        }
    }
    private Mesh mesh;
    private GameObject tableObject;
    private bool isTableAlreadyExists;

    private void Start() {
        mesh = GetComponent<MeshFilter>().mesh;
        setCoordinates();
        // set up the scalable script
        scalableScript = gameObject.AddComponent<Scalable>();
        scalableScript.ScalingSensitivity = 10f;
        scalableScript.minimumScale = 0.1f;

        scalableScript.OnRegisteringForScaling += ScalableScript_OnRegisteringForScaling;
        scalableScript.OnScalingUpdated += ScalableScript_OnScalingUpdated;

        gameObject.AddComponent<DeleteOnVoice>();
    }

    private void setCoordinates() {
        Coordinates = LocationHelper.worldPositionToGeoCoordinate(transform.position);
    }

    private void ScalableScript_OnScalingUpdated(bool isExceedingLimit) {
        NotifyHeightInfo(isExceedingLimit);
    }

    private void ScalableScript_OnRegisteringForScaling() {
        // might need to set the parent transform to something above the tile
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
    public void RegisterForScaling() {
        GestureManager.Instance.RegisterGameObjectForScalingUsingManipulation(scalableScript);
    }
    public void NotifyHeightInfo(bool isExceedingLimit) {
        if (cursor == null)
            cursor = GameObject.Find(GameObjectNamesHolder.NAME_CURSOR).GetComponent<CustomObjectCursor>();

        mesh.RecalculateBounds();
        float realWorldHeight = computeRealWorldHeight();
        int numOfStoreys = estimateNumOfStoreys(realWorldHeight);
        object[] arguments = { realWorldHeight, numOfStoreys, isExceedingLimit };
        cursor.UpdateCurrentHeightInfo(arguments);
    }
    #endregion

    private float computeRealWorldHeight() {
        float hologramHeight = transform.TransformVector(mesh.bounds.size).y;
        float realWorldHeight = hologramHeight / MapDataDisplay.Instance.MapWorldRelativeScale;
        return realWorldHeight;
    }

    private int estimateNumOfStoreys(float buildingHeight) {
        return (int)(buildingHeight / heightOfAStorey) + 1;
    }

    /// <summary>
    /// calculates the base area of the polygon in Unity space using shoelace method
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

    public void OnInputClicked(InputClickedEventData eventData) {
        ShowDetails();
        positionTableObject();
        FillTableData();
    }

    public void ShowDetails() {
        if (isTableAlreadyExists) {
            positionTableObject();
            return;
        }

        tableObject = Instantiate(PrefabHolder.Instance.tablePrefab);
        tableObject.transform.parent = gameObject.transform;
        positionTableObject();

        isTableAlreadyExists = true;
    }

    public void HideDetails() {
        if (!isTableAlreadyExists)
            return;

        Destroy(tableObject);
        tableObject = null;
        isTableAlreadyExists = false;
    }

    private void FillTableData() {
        string textToDisplay;
        //show area, height and coordinates
        string name = PrefabHolder.renderBold(PrefabHolder.changeTextSize(gameObject.name, 60));
        string area = PrefabHolder.renderBold("Area: ") + RealWorldArea;
        string coordinatesString = PrefabHolder.formatLatLong(Coordinates);
        textToDisplay = name + "\n" + area + "\n" + coordinatesString;
        tableObject.GetComponent<DraggableInfoTable>().FillTableData(textToDisplay);
    }

    private void positionTableObject() {
        float distanceRatio = 0.4f;
        tableObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        tableObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
        tableObject.SendMessage("UpdateLinePositions");
    }


}
