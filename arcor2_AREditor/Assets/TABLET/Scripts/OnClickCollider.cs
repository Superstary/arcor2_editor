using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnClickCollider : MonoBehaviour
{
    public GameObject Target;

    public void OnClick()
    {
        Target.GetComponent<Base.Clickable>().OnClick(Base.Clickable.Click.TOUCH);
    }

    public void OnMouseOver() {
        if (Input.GetMouseButtonDown(0)) {
            Target.GetComponent<Base.Clickable>().OnClick(Base.Clickable.Click.MOUSE_LEFT_BUTTON);
        }
        if (Input.GetMouseButtonDown(1)) {
            Target.GetComponent<Base.Clickable>().OnClick(Base.Clickable.Click.MOUSE_RIGHT_BUTTON);
        }
        if (Input.GetMouseButtonDown(2)) {
            Target.GetComponent<Base.Clickable>().OnClick(Base.Clickable.Click.MOUSE_MIDDLE_BUTTON);
        }
    }
}
