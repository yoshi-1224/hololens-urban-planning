using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;

public class DrawingManager : Singleton<DrawingManager>, IInputClickHandler {
    private GameObject cursor;
    
    /// <summary>
    /// stores the positions of instantiated spheres
    /// </summary>
    private List<Vector3> polygonVertices;

    /// <summary>
    /// transform parent of the instantiated spheres. A simple new gameobject created
    /// </summary>
    private GameObject pointsParent;

    /// <summary>
    /// message to the user that shows up when the polygon can be enclosed with a click
    /// </summary>
    public GameObject EncloseGuide;
    private GameObject instantiatedGuideObj;
    private float guidePositionAbovePoint = 0.05f;

    public Material polygonMaterial;

    public bool CanPolygonBeEnclosedAndCursorOnFirstPoint {
        get; set;
    }

    public GameObject SpherePrefab;
    private bool isAnyPointDrawnYet; // whether or not to call the update function to render line
    private LineRenderer currentlyDrawnLine;

    private GameObject lastDrawnPoint;
    private GameObject map;
    private float lineWidth = 0.002f;
	
    public void StartDrawing() {
        InteractibleMap.Instance.IsDrawing = true;
        GuideStatus.ShouldShowGuide = false;
        isAnyPointDrawnYet = false;
        polygonVertices = new List<Vector3>();
        changeCursorToDrawingPoint();
        InputManager.Instance.PushModalInputHandler(gameObject);
        CanPolygonBeEnclosedAndCursorOnFirstPoint = false;
        map = GameObject.Find("CustomizedMap");
    }

    public void StopDrawing() {
        GlobalVoiceCommands.Instance.IsInDrawingMode = false;
        InteractibleMap.Instance.IsDrawing = false;
        InputManager.Instance.PopModalInputHandler();
        clearPoints();
        changeCursorBack();
        isAnyPointDrawnYet = false;
        CanPolygonBeEnclosedAndCursorOnFirstPoint = false;
        GuideStatus.ShouldShowGuide = true;
    }

    /// <summary>
    /// used to render the lines. Is there any other use?
    /// </summary>
	void Update () {
        if (!isAnyPointDrawnYet || CanPolygonBeEnclosedAndCursorOnFirstPoint)
            return;
        UpdateLineDrawing();
	}

    /// <summary>
    /// Should be called from OnTriggerEnter/OnFocusEnter by FirstDrawnPoint script.
    /// </summary>
    public bool CheckCanPolygonBeEnclosed() {
        if (polygonVertices.Count < 3)
            return false;
        return true;
    }

    private void UpdateLineDrawing() {
        currentlyDrawnLine.SetPosition(1, GazeManager.Instance.HitPosition);
    }

    private void changeCursorToDrawingPoint() {
        cursor = GameObject.Find("CustomCursorWithFeedback");
        cursor.SendMessage("EnterDrawingMode");
    }

    private void changeCursorBack() {
        cursor.SendMessage("ExitDrawingMode");
    }

    /// <summary>
    /// the argument position should be the actual position that requires no
    /// further calculations or processing.
    /// </summary>
    /// <param name="position"></param>
    private void createSphereAt(Vector3 position) {
        GameObject sphere = Instantiate(SpherePrefab, position, Quaternion.identity);
        polygonVertices.Add(sphere.transform.position);
        lastDrawnPoint = sphere;
        if (!isAnyPointDrawnYet) {
            // if this sphere is the first sphere to be drawn
            pointsParent = new GameObject("PointsParent");
            setUpFirstSphere(sphere);
        }
        if (isAnyPointDrawnYet)
            currentlyDrawnLine.SetPosition(1, position);
        instantiateNewLine();
        sphere.transform.parent = pointsParent.transform;
    }

    private void setUpFirstSphere(GameObject firstSphere) {
        firstSphere.AddComponent<FirstDrawnPoint>();

    }

    /// <summary>
    /// args should take in pointsDrawn
    /// </summary>
    private void instantiatePolygon() {
        Dictionary<int, int> neighbouringVertexMapping;
        GameObject polygon = PolygonGenerator.GeneratePolygonFromVertices(polygonVertices, 0.1f, polygonMaterial, out neighbouringVertexMapping);
        polygon.transform.parent = GameObject.Find("LOD2").transform; // make map script more general: just do child stuff in script rather than assigning in editor
        ScalableHeight script = polygon.AddComponent<ScalableHeight>();
        script.neighbouringVertexMapping = neighbouringVertexMapping;
        polygon.AddComponent<DeleteOnVoice>();
        StopDrawing();
    }

    /// <summary>
    /// should be called when
    /// 1) cancelling the drawing action
    /// 2) the map is moved without confirming the instantiation of the polygon
    /// 3) polygon is created successfully
    /// </summary>
    private void clearPoints() {
        Destroy(pointsParent);
        polygonVertices.Clear();
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        if (CanPolygonBeEnclosedAndCursorOnFirstPoint) {
            instantiatePolygon();
            clearPoints();
            isAnyPointDrawnYet = false;
            CanPolygonBeEnclosedAndCursorOnFirstPoint = false;
        } else if (GazeManager.Instance.HitObject == map){
            Vector3 drawPointPosition = GameObject.FindGameObjectWithTag("DrawPoint").transform.position;
            createSphereAt(drawPointPosition);
            isAnyPointDrawnYet = true;
        } else {
            // do nothing
        }

    }

    private void instantiateNewLine() {
        currentlyDrawnLine = lastDrawnPoint.GetComponent<LineRenderer>();
        currentlyDrawnLine.positionCount = 2;
        currentlyDrawnLine.SetPosition(0, lastDrawnPoint.transform.position);
        currentlyDrawnLine.SetPosition(1, GazeManager.Instance.HitPosition);
        currentlyDrawnLine.startWidth = lineWidth;
        currentlyDrawnLine.endWidth = lineWidth;
    }

    public void FixLineEndAtFirstSphere() {
        currentlyDrawnLine.SetPosition(1, polygonVertices[0]);
    }

    public void instantiateGuide(Vector3 firstSpherePosition) {
        instantiatedGuideObj = Instantiate(EncloseGuide, firstSpherePosition + new Vector3(0, guidePositionAbovePoint, 0), Quaternion.LookRotation(firstSpherePosition - Camera.main.transform.position, Vector3.up));
        TextMesh textMesh = instantiatedGuideObj.GetComponent<TextMesh>();
        textMesh.fontSize = 56;
        textMesh.text = "Click to enclose this polygon";
    }

    public void destroyGuide() {
        if (instantiatedGuideObj != null)
            Destroy(instantiatedGuideObj);
        instantiatedGuideObj = null;
    }
}
