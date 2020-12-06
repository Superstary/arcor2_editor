using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;

public class TileHololens : MonoBehaviour
{
    [SerializeField]
    private TMPro.TMP_Text Label;

    [SerializeField] private GameObject MainButton;


    public void SetLabel(string label) {
        Label.text = label;
    }

    public string GetLabel() {
        return Label.text;
    }

    public void AddListener(UnityAction callback) {
        if (callback != null) {
            var InterableButton = MainButton.transform.GetComponent<Interactable>();
            InterableButton .OnClick.AddListener(callback);
        }
    }

    public virtual void InitTile(string tileLabel, UnityAction mainCallback) {
        SetLabel(tileLabel);
        if (mainCallback != null) {
            AddListener(mainCallback);
        }

    }


}
