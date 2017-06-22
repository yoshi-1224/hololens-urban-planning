using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideStatus : MonoBehaviour {

	public static bool ShouldShowGuide {
        get; set;
    }

    private void Start() {
        ShouldShowGuide = true;
    }
}
