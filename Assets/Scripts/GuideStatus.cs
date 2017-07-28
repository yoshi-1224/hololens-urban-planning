using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;

/// <summary>
/// This component is used for telling whether or not the user should receive a guide
/// object when gazed at the parent object for some time. This is available to any
/// script so that it can freely set and get the boolean value and the guide object.
/// </summary>
public class GuideStatus : Singleton<GuideStatus> {
    [SerializeField]
    private GameObject GuideObjectPrefab;
    public static GameObject GuideObjectInstance;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    public const float GazeDurationTillGuideDisplay = 4;

    public static bool ShouldShowGuide {
        get; set;
    }
    private static Text GuideText;

    protected override void Awake() {
        base.Awake();
        GuideObjectInstance = Instantiate(GuideObjectPrefab);
        GuideText = GuideObjectInstance.GetComponentInChildren<Text>();
        GuideObjectInstance.SetActive(false); // pool this object
        ShouldShowGuide = false;
    }

    public static void PositionGuideObject(Vector3 tableHostPosition) {
        float distanceRatio = 0.2f;
        GuideObjectInstance.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * tableHostPosition;
        GuideObjectInstance.transform.rotation = Quaternion.LookRotation(tableHostPosition - Camera.main.transform.position, Vector3.up);
    }

    public static void FillCommandDetails(string textToDisplay) {
        GuideText.text = "<b>Valid commands:</b>\n" + textToDisplay;
    }
}
