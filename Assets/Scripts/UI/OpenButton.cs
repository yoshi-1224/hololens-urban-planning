using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class OpenButton : ButtonBase {
    Animator anim;
    int openButtonTriggerHash = Animator.StringToHash("OpenButtons");
    int closeButtonTriggetHash = Animator.StringToHash("CloseButtons");
    private bool areButtonsShown;

    protected override void Awake() {
        base.Awake();
        anim = GetComponentInParent<Animator>();
        areButtonsShown = false;
    }

    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        if (!areButtonsShown)
            showButtons();
        else
            hideButtons();

    }

    private void showButtons() {
        if (areButtonsShown || isAnimationAlreadyPlaying())
            return;
        anim.SetTrigger(openButtonTriggerHash);
        areButtonsShown = true;
    }
   
    private void hideButtons() {
        if (!areButtonsShown || isAnimationAlreadyPlaying())
            return;
        DropDownBuildings.Instance.HideDropdownList();
        DropDownPinnedLocations.Instance.HideDropdownList();
        DropDownPrefabs.Instance.HideDropdown();
        anim.SetTrigger(closeButtonTriggetHash);
        areButtonsShown = false;
    }

    private bool isAnimationAlreadyPlaying() {
        return anim.GetCurrentAnimatorStateInfo(0).IsName("OpenPanButtons");
    }



}
