using System.Collections;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System.Text;

public class InteractibleBuilding : MonoBehaviour, IFocusable, ISpeechHandler, IInputClickHandler {
    private GameObject tableObjectInstance;
    private DraggableInfoTable tableObjectScript;
    private GameObject guideObjectInstance;

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

        hideGuideObject();
        HideTable();
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
        if (guideObjectInstance != null || !GuideStatus.ShouldShowGuide) //already exists
            yield break;

        // wait and then display
        yield return new WaitForSeconds(GuideStatus.GazeDurationTillGuideDisplay);
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
        StringBuilder str = new StringBuilder();
        if (GetComponent<DeleteOnVoice>() != null)
            str.AppendLine(DeleteOnVoice.COMMAND_DELETE);
        str.AppendLine(COMMAND_SHOW_DETAILS);
        str.AppendLine(COMMAND_HIDE_DETAILS);
        str.AppendLine(Movable.COMMAND_MOVE);
        str.Append(Rotatable.COMMAND_ROTATE);
        GuideStatus.FillCommandDetails(str.ToString());
    }

    #endregion

    #region gesture-related
    private void registerForUserGestures() {
        HideTable();
        hideGuideObject();
    }

    private void unregisterForUserGestures() {
    }

    #endregion

    #region table-related
    public void ShowTable() {
        if (isTableAlreadyExists) {
            tableObjectScript.PositionTableObject();
            return;
        }

        tableObjectInstance = PrefabHolder.Instance.GetPooledTable();
        tableObjectScript = tableObjectInstance.GetComponentInChildren<DraggableInfoTable>();
        tableObjectScript.tableHolderTransform = gameObject.transform; // do this before active
        tableObjectInstance.SetActive(true);

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
        tableObjectInstance.SetActive(false);
        tableObjectInstance = null;
        tableObjectScript = null;
        isTableAlreadyExists = false;
    }

    private void FillTableData() {
        string buildingName = BuildingManager.Instance.GetBuildingName(gameObject.name);
        string textToDisplay;
        if (buildingName == null) { // entry not found
            buildingName = "unknown";
            textToDisplay = "unknown";
        } else {
            textToDisplay = BuildingManager.Instance.GetBuildingInformation(gameObject.name);
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
