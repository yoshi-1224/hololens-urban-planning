using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
/// <summary>
/// This component is used for telling whether or not the user should receive a guide
/// object when gazed at the parent object for some time. This is available to any
/// script so that it can freely set and get the boolean value and the guide object.
/// </summary>

public class GuideStatus : Singleton<GuideStatus> {
    public GameObject GuideObjectPrefab;

	public static bool ShouldShowGuide {
        get; set;
    }

    private void Start() {
        ShouldShowGuide = true;
    }

    private static GameObject currentlyShownGuide;
    public static GameObject CurrentlyShownGuide {
        get {
            return currentlyShownGuide;
        }
        set {
            DestroyGuideIfShown();
            currentlyShownGuide = value;
        }
    }

    public static void DestroyGuideIfShown() {
        if (currentlyShownGuide != null)
            Destroy(currentlyShownGuide);
    }

    public static void PositionGuideObject(Vector3 tableHostPosition) {
        float distanceRatio = 0.2f;
        currentlyShownGuide.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * tableHostPosition;
        currentlyShownGuide.transform.rotation = Quaternion.LookRotation(tableHostPosition - Camera.main.transform.position, Vector3.up);
    }

    public static void fillGuideDetails(string textToDisplay) {
        currentlyShownGuide.GetComponentInChildren<Text>().text = textToDisplay;
    }
}
