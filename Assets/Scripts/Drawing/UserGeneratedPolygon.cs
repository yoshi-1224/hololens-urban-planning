using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using Mapbox.Utils;
using HoloToolkit.Unity;
using Mapbox.Unity.Utilities;

public class UserGeneratedPolygon : MonoBehaviour, ISpeechHandler, IInputClickHandler {
    public const string COMMAND_SCALE = "scale";
    private Transform parent;
    private float minimumHeight = 0.05f;
    private Vector3 previousManipulationPosition;
    private CustomObjectCursor cursor;
    private static float heightOfAStorey = 5;
    private Scalable scalableComponent;

    private Movable movableComponent;
    private Rotatable rotatableComponent;

    /// <summary>
    /// should be passed when this component is added to this gameObject
    /// </summary>
    public Dictionary<int, int> neighbouringVertexMapping {
        get; set;
    }

    private Vector2d Coordinates;
    public double RealWorldArea {
        get {
            return computeBaseArea();
        }
    }
    public float RealWorldHeight {
        get {
            return computeRealWorldHeight();
        }
    }
    private Mesh mesh;

    private GameObject tableObject;
    private DraggableInfoTable tableObjectScript;
    private bool isTableAlreadyExists;

    private void Start() {
        mesh = GetComponent<MeshFilter>().mesh;
        setCoordinates();
        // set up the scalable script
        scalableComponent = gameObject.AddComponent<Scalable>();
        scalableComponent.ScalingSensitivity = 10f;
        scalableComponent.minimumScale = 0.1f;

        scalableComponent.OnRegisteringForScaling += ScalableScript_OnRegisteringForScaling;
        scalableComponent.OnScalingUpdated += ScalableScript_OnScalingUpdated;

        gameObject.AddComponent<DeleteOnVoice>();
        movableComponent = gameObject.AddComponent<Movable>();
        movableComponent.OnUnregisterForTranslation += UnregisteForTranslation;

        rotatableComponent = gameObject.AddComponent<Rotatable>();

        if (InteractibleMap.Instance != null) {
            InteractibleMap.Instance.OnBeforeUserActionOnMap += InteractibleMap_OnBeforeMapPlacingStart;
            InteractibleMap.Instance.OnAfterUserActionOnMap += InteractibleMap_OnMapPlaced;
        }
    }

    private void OnDestroy() {
        if (InteractibleMap.Instance != null) {
            InteractibleMap.Instance.OnBeforeUserActionOnMap -= InteractibleMap_OnBeforeMapPlacingStart;
            InteractibleMap.Instance.OnAfterUserActionOnMap -= InteractibleMap_OnMapPlaced;
        }
    }

    private void InteractibleMap_OnMapPlaced() {
        gameObject.SetActive(true);
    }

    private void InteractibleMap_OnBeforeMapPlacingStart(bool shouldHideObjectToo) {
        HideTable();
        if (shouldHideObjectToo)
            gameObject.SetActive(false);
    }

    private void setCoordinates() {
        Coordinates = LocationHelper.WorldPositionToGeoCoordinate(transform.position);
    }

    private void ScalableScript_OnScalingUpdated(bool isExceedingLimit) {
        NotifyHeightInfo(isExceedingLimit);
    }

    private void ScalableScript_OnRegisteringForScaling() {
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_SCALE:
                RegisterForScaling();
                break;
        }
    }

    #region scaling-related
    private void RegisterForScaling() {
        HideTable();
        GestureManager.Instance.RegisterGameObjectForScalingUsingManipulation(scalableComponent);
    }

    private void UnregisteForTranslation() {
        setCoordinates();
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
    /// At the moment, GPR == numOfStoreys since GFA per floor == base area
    /// </summary>
    private float estimateGFA(int numOfStoreys, float siteArea) {
        return numOfStoreys * siteArea;
    }

    /// <summary>
    /// calculates the base area of the polygon in Unity space using shoelace method
    /// The value that this method returns should be scaled with map scale
    /// to convert into real-world area
    /// </summary>
    public double computeBaseArea() {
        if (neighbouringVertexMapping == null)
            return -1;

        double area = 0;
        // assumption is that the first half of the vertices of mesh represent
        // the top vertices and the second half the bottom vertices
        // we are going to use the top vertices since these are the ones we
        // have neighbouring vertex mapping for (shoelace method requires that
        // the vertices are ordered clockwise/anti-clockwise
        int lastTopVertexIndex = mesh.vertices.Length / 2;

        Vector2[] verticesIn2d = vector3Tovector2InWorldSpace(mesh.vertices);

        for (int i = 0; i < lastTopVertexIndex; i++) {
            int neighbourIndex = neighbouringVertexMapping[i];
            Vector2 thisPoint = verticesIn2d[i];
            Vector2d thisPointInRealWorld = Conversions.LatLonToMeters( LocationHelper.WorldPositionToGeoCoordinate(new Vector3(thisPoint.x, 0, thisPoint.y)));

            Vector2 neighbourPoint = verticesIn2d[neighbourIndex];
            Vector2d neighborPointInRealWorld = Conversions.LatLonToMeters(LocationHelper.WorldPositionToGeoCoordinate(new Vector3(neighbourPoint.x, 0, neighbourPoint.y)));

            area += (thisPointInRealWorld.x * neighborPointInRealWorld.y);
            area -= (thisPointInRealWorld.y * neighborPointInRealWorld.x);
        }

        return Math.Abs(area) / 2f;
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
        ShowTable();
    }

    public void ShowTable() {
        if (!isTableAlreadyExists) {
            tableObject = Instantiate(PrefabHolder.Instance.tablePrefab);
            tableObjectScript = tableObject.GetComponentInChildren<DraggableInfoTable>();
            tableObjectScript.tableHolderTransform = gameObject.transform;
            //subscribe to the button clicked event
            FillTableData();
            tableObjectScript.OnHideTableButtonClicked += HideTable;
            isTableAlreadyExists = true;
        }

        positionTableObject();
        
    }

    public void HideTable() {
        if (!isTableAlreadyExists)
            return;
        tableObjectScript.OnHideTableButtonClicked -= HideTable;
        Destroy(tableObject);
        tableObject = null;
        isTableAlreadyExists = false;
    }

    private void FillTableData() {
        string textToDisplay;
        //show area, height and coordinates
        string name = PrefabHolder.renderBold(PrefabHolder.changeTextSize(gameObject.name, 60));
        string area = PrefabHolder.renderBold("Area: \n") + string.Format("{0:0.00}m\xB2", RealWorldArea);
        string coordinatesString = PrefabHolder.formatLatLong(Coordinates);
        textToDisplay = area + "\n" + coordinatesString;
        tableObjectScript.FillTableData(name, textToDisplay);
    }

    private void positionTableObject() {
        float distanceRatio = 0.4f;
        Vector3 targetPosition = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        tableObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);

        tableObject.transform.position = gameObject.transform.position; // start from building
        tableObject.GetComponentInChildren<Interpolator>().SetTargetPosition(targetPosition);
        tableObject.GetComponentInChildren<Interpolator>().InterpolationDone += InteractibleBuilding_InterpolationDone;
    }

    private void InteractibleBuilding_InterpolationDone() {
        tableObjectScript.UpdateLinePositions();
        tableObject.GetComponentInChildren<Interpolator>().InterpolationDone -= InteractibleBuilding_InterpolationDone;
    }

}
