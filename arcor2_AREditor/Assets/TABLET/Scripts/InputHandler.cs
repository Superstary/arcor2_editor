using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using UnityEngine;
using UnityEngine.EventSystems;
using RuntimeGizmos;

public class InputHandler : Singleton<InputHandler> {

    public LayerMask LayerMask;

    public delegate void EventClick(object sender, EventClickArgs args);
    public event EventClick OnBlindClick;
    public event EventClick OnGeneralClick;
    public event EventHandler OnEscPressed;
    public event EventHandler OnEnterPressed;
    public event EventHandler OnDeletePressed;

    private bool longTouch = false;
    private bool pointerOverUI = false;
    private IEnumerator coroutine;

    private GameObject hoveredObject;

    private void Update() {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        HandleTouch();
#else
        HandleInputStandalone();
#endif
    }

    private void HandleInputStandalone() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            OnEscPressed?.Invoke(this, EventArgs.Empty);
        } else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
            OnEnterPressed?.Invoke(this, EventArgs.Empty);
        } else if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) {
            OnDeletePressed?.Invoke(this, EventArgs.Empty);
        }
        // Left Button
        if (Input.GetMouseButtonDown(0)) {
            if (TransformGizmo.Instance.mainTargetRoot != null) {
                if (TransformGizmo.Instance.translatingAxis == Axis.None) {
                    TransformGizmo.Instance.ClearTargets();
                    TryToRaycast(Clickable.Click.MOUSE_LEFT_BUTTON);
                }
            } else {
                TryToRaycast(Clickable.Click.MOUSE_LEFT_BUTTON);
            }
        }
        // Right Button
        else if (Input.GetMouseButtonDown(1)) {
            TryToRaycast(Clickable.Click.MOUSE_RIGHT_BUTTON);
        }
        // Middle Button
        else if (Input.GetMouseButtonDown(2)) {
            TryToRaycast(Clickable.Click.MOUSE_MIDDLE_BUTTON);
        } else {
            TryToRaycast(Clickable.Click.MOUSE_HOVER);
        }
    }

    private void TryToRaycast(Clickable.Click clickType) {
        // Do not raycast through UI element
        if (!EventSystem.current.IsPointerOverGameObject()) {
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, LayerMask)) {
               // try {
                    if (clickType == Clickable.Click.MOUSE_HOVER) {
                        //try {
                        
                            if (hoveredObject == null) {
                                hit.collider.transform.gameObject.SendMessage("OnHoverStart");
                                hoveredObject = hit.collider.transform.gameObject;
                            } else {
                                if (!GameObject.ReferenceEquals(hit.collider.transform.gameObject, hoveredObject)) {
                                    hoveredObject.SendMessage("OnHoverEnd");
                                    hit.collider.transform.gameObject.SendMessage("OnHoverStart");
                                    hoveredObject = hit.collider.transform.gameObject;
                                }
                            }
                        //} catch (Exception e) {
                          //  Debug.LogError(e);
                        //}
                    } else {
                        hit.collider.transform.gameObject.SendMessage("OnClick", clickType);
                        if (hoveredObject != null) {
                            hoveredObject.SendMessage("OnHoverEnd");
                            hoveredObject = null;
                        }
                        
                    }
              //  } catch (Exception e) {
              //      Debug.LogError(e);
              //  }
            } else {
                OnBlindClick?.Invoke(this, new EventClickArgs(clickType));
                if (hoveredObject != null) {
                    hoveredObject.SendMessage("OnHoverEnd");
                    hoveredObject = null;
                }
            }

            OnGeneralClick?.Invoke(this, new EventClickArgs(clickType));
        }
    }


    private void HandleTouch() {
        RaycastHit hit = new RaycastHit();
        /*foreach (Touch touch in Input.touches) {
            // Do not raycast through UI element
            if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId)) {

                if (touch.phase == TouchPhase.Began) {
                    if (coroutine != null)
                        StopCoroutine(coroutine);
                    coroutine = LongTouch(touch);
                    StartCoroutine(coroutine);

                    if (Physics.Raycast(Camera.main.ScreenPointToRay(touch.position), out hit, Mathf.Infinity, LayerMask)) {
                        try {
                            hit.collider.transform.gameObject.SendMessage("OnClick", Clickable.Click.TOUCH);
                        } catch (Exception e) {
                            Debug.LogError(e);
                        }
                    } else {
                        OnBlindClick?.Invoke(this, new EventClickArgs(Clickable.Click.TOUCH));
                    }

                    OnGeneralClick?.Invoke(this, new EventClickArgs(Clickable.Click.TOUCH));
                }

                // NOTE: TouchPhase.Ended always ignores UI clicking check (IsPointerOverGameObject)
                if (touch.phase == TouchPhase.Ended) {
                    if (longTouch) {
                        longTouch = false;

                    } else {
                        if (coroutine != null)
                            StopCoroutine(coroutine);
                        longTouch = false;

                        if (Physics.Raycast(Camera.main.ScreenPointToRay(touch.position), out hit, Mathf.Infinity, LayerMask)) {
                            try {
                                hit.collider.transform.gameObject.SendMessage("OnClick", Clickable.Click.TOUCH_ENDED);
                            } catch (Exception e) {
                                Debug.LogError(e);
                            }
                        } else {
                            OnBlindClick?.Invoke(this, new EventClickArgs(Clickable.Click.TOUCH_ENDED));
                        }

                        OnGeneralClick?.Invoke(this, new EventClickArgs(Clickable.Click.TOUCH_ENDED));
                    }
                }
            }
        }*/
        if (!GameManager.Instance.SceneInteractable)
            return;
        foreach (Touch touch in Input.touches) {
            if (touch.phase == TouchPhase.Began) {
                // This is only valid in Began phase. During end phase it always return false
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) {
                    // skip if clicking on GUI object (e.g. controlbox)
                    pointerOverUI = true;
                    continue;                    
                }
                pointerOverUI = false;
                if (coroutine != null)
                    StopCoroutine(coroutine);
                coroutine = LongTouch(touch);
                StartCoroutine(coroutine);
            } else if (touch.phase == TouchPhase.Ended) {
                if (pointerOverUI)
                    return;
                if (longTouch) {
                    longTouch = false;
                } else {
                    if (coroutine != null)
                        StopCoroutine(coroutine);
                    longTouch = false;
                    if (TransformGizmo.Instance.mainTargetRoot != null) {
                        if (TransformGizmo.Instance.translatingAxis == Axis.None) {
                            TransformGizmo.Instance.ClearTargets();
                            Sight.Instance.Touch();
                        }
                    } else {
                        Sight.Instance.Touch();
                    }                    
                }
            }
        }
    }

    private IEnumerator LongTouch(Touch touch) {
        yield return new WaitForSeconds(1f);
        longTouch = true;
        TransformGizmo.Instance.ClearTargets();
        Sight.Instance.LongTouch();

        /*
        yield return new WaitForSeconds(3f);

        longTouch = true;
        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast(Camera.main.ScreenPointToRay(touch.position), out hit, Mathf.Infinity, LayerMask)) {
            try {
                hit.collider.transform.gameObject.SendMessage("OnClick", Clickable.Click.LONG_TOUCH);
            } catch (Exception e) {
                Debug.LogError(e);
            }
        } else {
            OnBlindClick?.Invoke(this, new EventClickArgs(Clickable.Click.LONG_TOUCH));
        }

        OnGeneralClick?.Invoke(this, new EventClickArgs(Clickable.Click.LONG_TOUCH));
        */
    }

}

public class EventClickArgs : EventArgs {

    public Clickable.Click ClickType;

    public EventClickArgs(Clickable.Click clickType) {
        ClickType = clickType;
    }
}

