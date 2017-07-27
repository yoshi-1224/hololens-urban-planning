using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using Mapbox.Utils;
using HoloToolkit.Unity;

public class InteractibleBuilding : MonoBehaviour, IFocusable, ISpeechHandler, IInputClickHandler {
    private GameObject tableObject;
    private DraggableInfoTable tableObjectScript;
    private GameObject guideObject;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    public float gazeDurationTillGuideDisplay = 4;

    private bool isTableAlreadyExists;
    private bool isThisObjectShowingGuide;

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
        rotatableComponent.OnUnregisterForRotation += unregisterForUserGestures;
        rotatableComponent.OnRegisteringForRotation += registerForUserGestures;
        
        movableComponent = gameObject.AddComponent<Movable>();
        movableComponent.OnUnregisterForTranslation += registerForUserGestures;
        movableComponent.OnRegisteringForTranslation += registerForUserGestures;

        isTableAlreadyExists = false;
        isThisObjectShowingGuide = false;
        InteractibleMap.Instance.OnBeforeUserActionOnMap += InteractibleMap_OnBeforeUserActionOnMap;
        InteractibleMap.Instance.OnAfterUserActionOnMap += InteractibleMap_OnAfterUserActionOnMap;
    }

    private void InteractibleMap_OnAfterUserActionOnMap() {
        gameObject.SetActive(true);
    }

    private void InteractibleMap_OnBeforeUserActionOnMap(bool shouldHideObjectToo) {
        HideTable();
        if (shouldHideObjectToo)
            gameObject.SetActive(false);
    }

    private void OnDestroy() {
        if (movableComponent != null) {
            movableComponent.OnUnregisterForTranslation -= unregisterForUserGestures;
            movableComponent.OnRegisteringForTranslation -= registerForUserGestures;
        }

        if (rotatableComponent != null) {
            rotatableComponent.OnUnregisterForRotation -= unregisterForUserGestures;
            rotatableComponent.OnRegisteringForRotation -= registerForUserGestures;
        }

        if (InteractibleMap.Instance != null) {
            InteractibleMap.Instance.OnBeforeUserActionOnMap -= InteractibleMap_OnBeforeUserActionOnMap;
            InteractibleMap.Instance.OnAfterUserActionOnMap -= InteractibleMap_OnAfterUserActionOnMap;
        }
    }

    public void OnFocusEnter() {
        if (GuideStatus.ShouldShowGuide)
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
                ShowTable();
                break;
            case COMMAND_HIDE_DETAILS:
                HideTable();
                break;
        }
    }

#region guide-related

    /// <summary>
    /// waits for gazeDurationTillGuideDisplay seconds and then display the command guide
    /// </summary>
    IEnumerator ShowGuideCoroutine() {
        if (guideObject != null || !GuideStatus.ShouldShowGuide) //already exists
            yield break;

        // wait and then display
        yield return new WaitForSeconds(gazeDurationTillGuideDisplay);
        if (GuideStatus.ShouldShowGuide)
            showGuideObject();
    }

    private void showGuideObject() {
        if (isThisObjectShowingGuide)
            return;

        GuideStatus.GuideObjectInstance.SetActive(true);
        fillGuideDetails();
        GuideStatus.PositionGuideObject(transform.position);
        isThisObjectShowingGuide = true;
    }

    private void hideGuideObject() {
        if (isThisObjectShowingGuide && GuideStatus.GuideObjectInstance.activeSelf) {
            GuideStatus.GuideObjectInstance.SetActive(false);
            isThisObjectShowingGuide = false;
        }
    }

    private void fillGuideDetails() {
        string text = "<b>Valid commands:</b>\n" + COMMAND_SHOW_DETAILS + "\n" +
        COMMAND_HIDE_DETAILS + "\n" + Movable.COMMAND_MOVE + "\n" + Rotatable.COMMAND_ROTATE;
        if (GetComponent<DeleteOnVoice>() != null)
            text += "\n" + DeleteOnVoice.COMMAND_DELETE;
        GuideStatus.fillGuideDetails(text);
    }

    private void AllowGuideObject() {
        GuideStatus.ShouldShowGuide = true;
    }

    private void DisallowGuideObject() {
        GuideStatus.ShouldShowGuide = false;
        hideGuideObject();
    }
    #endregion

    #region gesture-related
    private void registerForUserGestures() {
        HideTable();
        DisallowGuideObject();
    }

    private void unregisterForUserGestures() {
        AllowGuideObject();
    }

    #endregion

    #region table-related
    public void ShowTable() {
        if (isTableAlreadyExists) {
            tableObjectScript.PositionTableObject();
            return;
        }

        tableObject = PrefabHolder.Instance.GetPooledTable();
        tableObjectScript = tableObject.GetComponentInChildren<DraggableInfoTable>();
        tableObjectScript.tableHolderTransform = gameObject.transform; // do this before active
        tableObject.SetActive(true);

        //subscribe to the button clicked event
        tableObjectScript.OnHideTableButtonClicked += HideTable;
        FillTableData();
        tableObjectScript.TableHolderHasGazeFeedback = true;
        isTableAlreadyExists = true;
        
        hideGuideObject();
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
        string buildingName = gameObject.name;
        string textToDisplay;
        TableDataHolder.TableData data;
        if (TableDataHolder.Instance.dataDict.TryGetValue(buildingName, out data)) {
            string _class = "<b>Class</b> : " + "\n" + data.building_class;
            string GPR = "<b>Gross Plot Ratio</b> : " + "\n" + data.GPR;
            if (data.building_name == "Chinese Culture Centre") {
                string type = "(Prefab Type " + data.storeys_above_ground + ")";
                //textToDisplay = name + "\n" + type + "\n\n" + _class + "\n" + GPR;
                textToDisplay = type + "\n\n" + _class + "\n" + GPR;
            } else {
                string measured_height = "<b>Measured Height</b> : " + "\n" + data.measured_height + "m";
                string numStoreys = "<b>Number of Storeys</b> : " + "\n" + data.storeys_above_ground;
                Vector2d coordinates = BuildingManager.Instance.GameObjectsInScene[buildingName].coordinates;
                string coordinatesString = Utils.FormatLatLong(coordinates);
                //textToDisplay = name + "\n\n" + _class + "\n" + GPR + "\n" + measured_height + "\n" + numStoreys + "\n" + coordinatesString;
                textToDisplay = _class + "\n" + GPR + "\n" + measured_height + "\n" + numStoreys + "\n" + coordinatesString;
            }
        } else {
            textToDisplay = "status unknown";
        }
        tableObjectScript.FillTableData(buildingName, textToDisplay);
    }

    #endregion

    public void OnInputClicked(InputClickedEventData eventData) {
        // don't set the table to be child of building since it propagates the click event
        ShowTable();
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
