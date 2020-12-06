using System.Collections;
using System.Collections.Generic;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class ActionPointHololens : ActionPoint3D
{
    // Start is called before the first frame update
    void Start()
    {
        base.Start();

        var interactable = gameObject.GetComponent<Interactable>();
        interactable .OnClick.AddListener(Manipulation);
    }

    private void Manipulation() {
        if (!enabled)
            return;
        if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingActionPoint ||
            GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingActionPointParent) {
            GameManager.Instance.ObjectSelected(this);
            return;
        }
        if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal) {
            return;
        }
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor) {
            Notifications.Instance.ShowNotification("Not allowed", "Editation of action point only allowed in project editor");
            return;
        }
        StartManipulation();
        // HANDLE MOUSE
        /*if (type == Click.MOUSE_LEFT_BUTTON || type == Click.LONG_TOUCH) {
            StartManipulation();
        } else if (type == Click.MOUSE_RIGHT_BUTTON || type == Click.TOUCH) {
            ShowMenu(false);
            tfGizmo.ClearTargets();
            outlineOnClick.GizmoUnHighlight();
        }*/
    }
}
