using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This component is used for telling whether or not the user should receive a guide
/// object when gazed at the parent object for some time. This is available to any
/// script so that it can freely set and get the boolean value.
/// Sample use case:
/// The user is rotating an object. During this rotation session, even if the user's 
/// gaze strays away to other object, that object should not show its guide to the user
/// as the user is in the middle of an action
/// </summary>
public class GuideStatus : MonoBehaviour {

	public static bool ShouldShowGuide {
        get; set;
    }

    private void Start() {
        ShouldShowGuide = true;
    }
}
