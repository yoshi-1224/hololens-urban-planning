using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;

/// <summary>
/// This class is to be attached to a polygon game object created by user. It enables height and area calculations and scaling
/// </summary>
public class UserGeneratedPolygon : MonoBehaviour, ISpeechHandler, IInputClickHandler, IFocusable {
    public const string COMMAND_SCALE = "scale";
    private float minimumHeightScale = 0.01f;
    private Vector3 previousManipulationPosition;
    private static float heightOfAStorey = 5;

    private Scalable scalableComponent;
    private Movable movableComponent;
    private Rotatable rotatableComponent;

    /// <summary>
    /// should be passed when this component is added to this gameObject. This is to be used to calculate base area using shoelace method
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
        scalableComponent.minimumScale = minimumHeightScale;

        scalableComponent.OnRegisteringForScaling += ScalableScript_OnRegisteringForScaling;
        scalableComponent.OnScalingUpdated += ScalableScript_OnScalingUpdated;

        gameObject.AddComponent<DeleteOnVoice>().OnBeforeDelete += DeleteOnVoiceComponent_OnBeforeDelete;
        movableComponent = gameObject.AddComponent<Movable>();
        movableComponent.OnUnregisterForTranslation += UnregisterForTranslation;

        rotatableComponent = gameObject.AddComponent<Rotatable>();

        if (InteractibleMap.Instance != null) {
            InteractibleMap.Instance.OnBeforeUserActionOnMap += InteractibleMap_OnBeforeMapPlacingStart;
            InteractibleMap.Instance.OnAfterUserActionOnMap += InteractibleMap_OnMapPlaced;
        }
    }

    private void DeleteOnVoiceComponent_OnBeforeDelete(DeleteOnVoice component) {
        component.OnBeforeDelete -= DeleteOnVoiceComponent_OnBeforeDelete;
        // delete this object from the list
        if (DropDownPolygons.Instance != null)
            DropDownPolygons.Instance.OnItemDeleted(gameObject.name);
    }

    private void OnDestroy() {
        if (InteractibleMap.Instance != null) {
            InteractibleMap.Instance.OnBeforeUserActionOnMap -= InteractibleMap_OnBeforeMapPlacingStart;
            InteractibleMap.Instance.OnAfterUserActionOnMap -= InteractibleMap_OnMapPlaced;
        }

        if (scalableComponent != null) {
            scalableComponent.OnRegisteringForScaling -= ScalableScript_OnRegisteringForScaling;
            scalableComponent.OnScalingUpdated -= ScalableScript_OnScalingUpdated;
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

    private void RegisterForScaling() {
        HideTable();
        GestureManager.Instance.RegisterGameObjectForScalingUsingManipulation(scalableComponent);
    }

    private void UnregisterForTranslation() {
        setCoordinates();

        // update the coordinate info stored in PolygonManager
        CoordinateBoundObject thisPolygon = PolygonManager.Instance.GameObjectsInScene[gameObject.name];
        thisPolygon.latitude = (float) Coordinates.x;
        thisPolygon.longitude = (float)Coordinates.y;
    }

    /// <summary>
    /// display its height info to the user during the scaling
    /// </summary>
    public void NotifyHeightInfo(bool isExceedingLimit) {
        mesh.RecalculateBounds();
        float realWorldHeight = computeRealWorldHeight();
        string text = string.Format("Height: {000:0.0}m", realWorldHeight);
        text += string.Format("\nnumber of storeys = {0:00}", estimateNumOfStoreys(realWorldHeight));
        ScreenMessageManager.Instance.DisplayMessage(text, Color.black);
    }

    private float computeRealWorldHeight() {
        float hologramHeight = transform.TransformVector(mesh.bounds.size).y;
        float realWorldHeight = hologramHeight / MapDataDisplay.Instance.MapWorldRelativeScale;
        return realWorldHeight;
    }

    private int estimateNumOfStoreys(float buildingHeight) {
        return (int)(buildingHeight / heightOfAStorey) + 1;
    }

    /// <summary>
    /// calculates the real-world base area of the polygon in Unity space using shoelace method
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
        if (isTableAlreadyExists) {
            tableObjectScript.PositionTableObject();
            return;
        }

        tableObject = PrefabHolder.Instance.GetPooledTable();
        tableObjectScript = tableObject.GetComponentInChildren<DraggableInfoTable>();
        tableObjectScript.tableHolderTransform = gameObject.transform; // do this before setting it active
        tableObject.SetActive(true);

        //subscribe to the button clicked event
        FillTableData();
        tableObjectScript.OnHideTableButtonClicked += HideTable;
        isTableAlreadyExists = true;

    }

    public void HideTable() {
        if (!isTableAlreadyExists)
            return;
        tableObjectScript.OnHideTableButtonClicked -= HideTable;
        tableObject.SetActive(false);
        tableObject = null;
        tableObjectScript = null;
        isTableAlreadyExists = false;
    }

    private void FillTableData() {
        string textToDisplay;

        //show area, height and coordinates
        string name = Utils.RenderBold(Utils.ChangeTextSize(gameObject.name, 60));
        string area = Utils.RenderBold("Area: \n") + string.Format("{0:0.00}m\xB2", RealWorldArea);

        string height = Utils.RenderBold("Height: \n") + Utils.FormatNumberInDecimalPlace(RealWorldHeight, 2);

        string coordinatesString = Utils.FormatLatLong(Coordinates);

        textToDisplay = area + "\n" + height + "m\n" + coordinatesString;
        tableObjectScript.FillTableData(name, textToDisplay);
    }

    public void OnFocusEnter() {
    }

    public void OnFocusExit() {
    }
}
