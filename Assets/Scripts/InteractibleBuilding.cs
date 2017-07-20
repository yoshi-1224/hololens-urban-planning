using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using Mapbox.Utils;
/// <summary>
/// This handles the voice commands as well as the gesture inputs on a building.
/// </summary>

public class InteractibleBuilding : MonoBehaviour, IFocusable, ISpeechHandler, IInputClickHandler {
    private GameObject tableObject;
    private GameObject guideObject;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    public float gazeDurationTillGuideDisplay = 4;

    public static bool shouldShowGuide {
        get {
            return GuideStatus.ShouldShowGuide;
        } set {
            GuideStatus.ShouldShowGuide = value;
        }
    }

    private bool isTableAlreadyExists;

    /// <summary>
    /// recognised voice commands. Make sure they are all in lower case
    /// </summary>
    private const string COMMAND_SHOW_DETAILS = "show info";
    private const string COMMAND_HIDE_DETAILS = "hide info";
    
    /// <summary>
    /// used for visual feedback when focus has entered/exited this gameobject.
    /// </summary>
    private Material[] defaultMaterials;
    private Rotatable rotatableComponent;
    private Movable movableComponent;

    private void Start() {
        Renderer tempRenderer = GetComponentInChildren<Renderer>();
        if (tempRenderer != null)
            defaultMaterials = tempRenderer.materials;
        if (defaultMaterials != null) {
            for (int i = 0; i < defaultMaterials.Length; i++) {
                defaultMaterials[i].SetColor("_EmissionColor", new Color(0.1176471f, 0.1176471f, 0.1176471f));
            }
        }
        rotatableComponent = gameObject.AddComponent<Rotatable>();
        rotatableComponent.OnUnregisterForRotation += RotatableComponent_OnUnregisterForRotation;
        movableComponent = gameObject.AddComponent<Movable>();
        movableComponent.OnUnregisterForTranslation += MovableComponent_OnUnregisterForTranslation;
        isTableAlreadyExists = false;

        InteractibleMap.Instance.OnBeforeMapPlacingStart += Instance_OnBeforeMapPlacingStart;
        InteractibleMap.Instance.OnMapPlaced += Instance_OnMapPlaced;
    }

    private void Instance_OnMapPlaced() {
        gameObject.SetActive(true);
    }

    private void Instance_OnBeforeMapPlacingStart() {
        HideDetails();
        gameObject.SetActive(false);

    }

    private void MovableComponent_OnUnregisterForTranslation() {
        AllowGuideObject();
    }

    private void RotatableComponent_OnUnregisterForRotation() {
        AllowGuideObject();
    }

    private void OnDestroy() {
        if (movableComponent != null)
            movableComponent.OnUnregisterForTranslation -= MovableComponent_OnUnregisterForTranslation;

        if (rotatableComponent != null)
            rotatableComponent.OnUnregisterForRotation -= RotatableComponent_OnUnregisterForRotation;

        if (InteractibleMap.Instance != null) {
            InteractibleMap.Instance.OnBeforeMapPlacingStart -= Instance_OnBeforeMapPlacingStart;
            InteractibleMap.Instance.OnMapPlaced -= Instance_OnMapPlaced;
        }
    }

    public void OnFocusEnter() {
        if (shouldShowGuide)
            StartCoroutine("ShowGuideCoroutine");
        EnableEmission();
    }

    public void OnFocusExit() {
        DisableEmission();
        hideGuideObject();
        StopCoroutine("ShowGuideCoroutine");
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_SHOW_DETAILS:
                ShowDetails();
                break;

            case COMMAND_HIDE_DETAILS:
                HideDetails();
                break;

            case Movable.COMMAND_MOVE:
                registerForTranslation();
                break;

            case Rotatable.COMMAND_ROTATE:
                registerForRotation();
                break;

            default:
                // just ignore
                break;
        }
    }

#region guide-related

    /// <summary>
    /// waits for gazeDurationTillGuideDisplay seconds and then display the command guide
    /// </summary>
    IEnumerator ShowGuideCoroutine() {
        if (guideObject != null || !shouldShowGuide) //already exists
            yield break;
        // wait and then display
        yield return new WaitForSeconds(gazeDurationTillGuideDisplay);
        if (shouldShowGuide)
            showGuideObject();
    }

    private void showGuideObject() {
        if (guideObject == null) {
            guideObject = Instantiate(PrefabHolder.Instance.guidePrefab);
            fillGuideDetails();
            guideObject.transform.parent = transform;
        }
        positionGuideObject();
    }

    private void hideGuideObject() {
        if (guideObject != null)
            Destroy(guideObject);
        guideObject = null;
    }

    private void fillGuideDetails() {
        TextMesh textMesh = guideObject.GetComponent<TextMesh>();
        textMesh.text =
            "<b>Valid commands:</b>\n" + COMMAND_SHOW_DETAILS + "\n" + 
            COMMAND_HIDE_DETAILS + "\n" + Movable.COMMAND_MOVE + "\n" + Rotatable.COMMAND_ROTATE;

        if (GetComponent<DeleteOnVoice>() != null)
            textMesh.text += "\n" + DeleteOnVoice.COMMAND_DELETE;
        textMesh.fontSize = 55;
        float scale = 0.003f;
        guideObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    private void positionGuideObject() {
        float distanceRatio = 0.2f;
        guideObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        guideObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
    }

    private void AllowGuideObject() {
        shouldShowGuide = true;
    }

    private void DisallowGuideObject() {
        shouldShowGuide = false;
        hideGuideObject();
    }

    #endregion

    #region translation-related
    /// <summary>
    /// register this object as the one in focus for rotation
    /// </summary>
    private void registerForTranslation() {
        HideDetails();
        DisallowGuideObject();
        GestureManager.Instance.RegisterGameObjectForTranslation(movableComponent);
    }

#endregion

    #region rotation-related
    /// <summary>
    /// register this object as the one in focus for rotation
    /// </summary>
    private void registerForRotation() {
        HideDetails();
        GestureManager.Instance.RegisterGameObjectForRotation(rotatableComponent);
        DisallowGuideObject();
    }

#endregion

    #region table-related
    public void ShowDetails() {
        if (isTableAlreadyExists) {
            positionTableObject();
            return;
        }

        tableObject = Instantiate(PrefabHolder.Instance.tablePrefab);
        tableObject.transform.SetParent(gameObject.transform, true);
        positionTableObject();
        FillTableData();
        isTableAlreadyExists = true;

        hideGuideObject();
    }

    public void HideDetails() {
        if (!isTableAlreadyExists)
            return;

        Destroy(tableObject);
        tableObject = null;
        isTableAlreadyExists = false;
    }

    private void positionTableObject() {
        float distanceRatio = 0.4f;
        tableObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        tableObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
        tableObject.SendMessage("UpdateLinePositions");
    }

    private void FillTableData() {
        string buildingName = gameObject.name;
        string textToDisplay;
        TableDataHolder.TableData data;
        if (TableDataHolder.Instance.dataDict.TryGetValue(buildingName, out data)) {
            string name = "<size=60><b>" + data.building_name + "</b></size>";
            string _class = "<b>Class</b> : " + data.building_class;
            string GPR = "<b>Gross Plot Ratio</b> : " + data.GPR;
            if (data.building_name == "Chinese Culture Centre") {
                string type = "(Prefab Type " + data.storeys_above_ground + ")";
                textToDisplay = name + "\n" + type + "\n\n" + _class + "\n" + GPR;
            } else {
                string measured_height = "<b>Measured Height</b> : " + data.measured_height + "m";
                string numStoreys = "<b>Number of Storeys</b> : " + data.storeys_above_ground;
                Vector2d coordinates = TableDataHolder.Instance.nameToLocation[buildingName];
                string coordinatesString = PrefabHolder.formatLatLong(coordinates);
                textToDisplay = name + "\n\n" + _class + "\n" + GPR + "\n" + measured_height + "\n" + numStoreys + "\n" + coordinatesString;
            }
        } else {
            textToDisplay = "status unknown";
        }
        tableObject.GetComponent<DraggableInfoTable>().FillTableData(textToDisplay);
    }

    #endregion

    public void OnInputClicked(InputClickedEventData eventData) {
        ShowDetails();
    }

#region visual feedbacks
    /// <summary>
    /// enable emission so that when this building is focused the material lights up
    /// to give the user visual feedback
    /// </summary>
    public void EnableEmission() {
        if (defaultMaterials == null)
            return;
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].EnableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// disable emission when gaze is exited from this building
    /// </summary>
    public void DisableEmission() {
        if (defaultMaterials == null)
            return;
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].DisableKeyword("_EMISSION");
        }
    }

#endregion

}
