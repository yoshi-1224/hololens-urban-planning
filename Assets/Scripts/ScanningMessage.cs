using HoloToolkit.Unity;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScanningMessage : MonoBehaviour, IInputClickHandler {

    private float Distance = 1.0f;
    private Interpolator interpolator;
    private Transform cameraTransform;
    private Text loadingMessage;
    private bool allowToTap;
    private bool textSet;
    AsyncOperation loadingOperation;

    private void Start() {
        interpolator = GetComponent<Interpolator>();
        loadingMessage = GetComponentInChildren<Text>();
        loadingMessage.text = "Scene Loading";
        interpolator.PositionPerSecond = 2f;
        cameraTransform = Camera.main.transform;
        StartCoroutine(LoadScene());
        allowToTap = false;
        textSet = false;
    }

    void LateUpdate() { 
        interpolator.SetTargetPosition(cameraTransform.position + (cameraTransform.forward * Distance));
        interpolator.SetTargetRotation(Quaternion.LookRotation(cameraTransform.forward, -cameraTransform.up));
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        // dismiss
        if (allowToTap) {
            loadingOperation.allowSceneActivation = true;
        }
    }

    public IEnumerator LoadScene() {
        yield return new WaitForSeconds(1f);
        loadingOperation = SceneManager.LoadSceneAsync(1);
        loadingOperation.allowSceneActivation = false;

        while(!loadingOperation.isDone) {
            if (loadingOperation.progress == 0.9f) { // must be 0.9f set by Unity
                if (!textSet) {
                    loadingMessage.text = "Scene Loaded. Tap to Continue";
                    textSet = true;
                    allowToTap = true;
                }
            }

            yield return null;
        }

    }
}
