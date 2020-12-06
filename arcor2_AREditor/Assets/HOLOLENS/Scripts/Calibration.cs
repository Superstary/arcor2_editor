using System;
using System.Collections;
using Base;
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.XR.WSA.Persistence;

public class Calibration :  Singleton<Calibration> {
    [SerializeField]
    /*top*/
    protected GameObject topMarker;
    [SerializeField]
    /*center*/
    protected GameObject centerMarker;
    [SerializeField]
    /*right*/
    protected GameObject rightMarker;

    public GameObject worldAnchor;
    public GameObject worldAnchorVisualizationCube;


    private Vector3 position1;
    private Vector3 position2;
    private Vector3 position3;

    private Boolean checkedTopMarker;
    private Boolean checkedCenterMarker;
    private Boolean checkedRightMarker;


    bool awaitingStore = true;
    public WorldAnchorManager worldAnchorManager;
    WorldAnchorStore store = null;

    private string[] anchors;
    private Boolean calibrated = false;

    private void Awake() {

        var button1 = topMarker.transform.GetChild(0).GetComponent<Interactable>();
        button1 .OnClick.AddListener(CheckMarkerTop);

        var button2 = centerMarker.transform.GetChild(0).GetComponent<Interactable>();
        button2 .OnClick.AddListener(CheckMarkerCenter);

        var button3 = rightMarker.transform.GetChild(0).GetComponent<Interactable>();
        button3 .OnClick.AddListener(CheckMarkerRight);
        WorldAnchorStore.GetAsync(StoreLoaded);
        /*WorldAnchorManager worldAnchorManager = new WorldAnchorManager();*/

    }

    private void StoreLoaded(WorldAnchorStore store)
    {
        this.store = store;
        awaitingStore = false;
    }

    void Start() {

        calibrated = false;
        checkedTopMarker = false;
        checkedCenterMarker = false;
        checkedRightMarker = false;
        topMarker.GetComponent<TrackedImageManager>().enabled = true;
        centerMarker.GetComponent<TrackedImageManager>().enabled = true;
        rightMarker.GetComponent<TrackedImageManager>().enabled = true;
#if UNITY_EDITOR
        SetupAnchor();
#endif
    }

    void Update()
    {
        if (!awaitingStore)
        {
            Debug.Log("AnchorMaker: Store loaded.");
            awaitingStore = true;
            anchors = this.store.GetAllIds();
            Debug.Log("Found anchors: " + anchors.Length);
            for (int index = 0; index < anchors.Length; index++)
            {
                Debug.Log(anchors[index]);
            }
           // LoadGame();
        }

    }

    private void Calibrating() {
     //   SetVuforiaActive(true);

     //   HideActiveChildrenObjects(true, worldAnchor);


        worldAnchor.transform.position = centerMarker.transform.position;
        worldAnchor.transform.rotation = centerMarker.transform.rotation;


        //get directions from anchor to reference markers
        //
        //          ROBOT
        //   11 ------------- 12
        //    |               |
        //    |               |
        //   10 ------------- 13
        Vector3 pp03 = rightMarker.transform.position - worldAnchor.transform.position;
        Vector3 pp01 = topMarker.transform.position - worldAnchor.transform.position;
        Vector3 n = Vector3.Cross(pp03, pp01);
        Matrix4x4 m = new Matrix4x4(new Vector4(pp03.x, pp01.x, n.x, 0),
                                    new Vector4(pp03.y, pp01.y, n.y, 0),
                                    new Vector4(pp03.z, pp01.z, n.z, 0),
                                    new Vector4(0, 0, 0, 1));

        worldAnchor.transform.rotation = m.inverse.rotation;
        //rotate around x axis to inverse Y axis
        worldAnchor.transform.Rotate(-90f, 0f, 0f, Space.Self);


        //apply offset to anchor due to marker paper
        //worldAnchorVisualizationCube.transform.localPosition = new Vector3(-0.208f, 0.121f, 0f);
        worldAnchorVisualizationCube.transform.localPosition = new Vector3(0f, 0f, 0f);
        worldAnchor.transform.position = worldAnchorVisualizationCube.transform.position;

        Debug.Log("POsition WORLD ANCHOR" + worldAnchor.gameObject.transform.position);
        string name = worldAnchorManager.AttachAnchor(worldAnchor);
        Debug.Log("Added anchor: " + name);

    }

    public void SetupAnchor() {
        Debug.Log("Calibration ...");

        #if UNITY_EDITOR
                   // if (!calibrated) {
                        calibrated = true;

                        //StartCoroutine(startFakeCalibration());
                        //GameObject super_root = new GameObject("super_root");
                        //super_root.transform.position = Vector3.zero;
                        //worldAnchor.transform.parent = super_root.transform;
                        //super_root.transform.eulerAngles = new Vector3(-90f, 0f, 0f);
                        worldAnchor.transform.position = new Vector3(0.2f,0.2f, 0.0f);
                     //   worldAnchor.transform.eulerAngles = new Vector3(-90f, 20f, 0f);
                        //worldAnchor.transform.Rotate(180f, 0f, 0f, Space.Self);

                        worldAnchorVisualizationCube.gameObject.SetActive(true);
                    //    worldAnchorRecalibrationButton.gameObject.SetActive(true);
                    //    if (OnSystemStarted != null) {
                    //        OnSystemStarted();
                    //    }
                  //  }
                    //TODO: will be removed in UNITY 2020.1
        #endif
       #if !UNITY_EDITOR
                    Debug.Log(store.anchorCount);
                    if (store.anchorCount > 0) {
                        string[] ids = store.GetAllIds();
                        //world anchor is present
                        if (ids.Length == 1) {
                            //attaching an existing anchor name will load it instead
                            worldAnchorManager.AttachAnchor(worldAnchor.gameObject, ids[0]);
                            worldAnchorVisualizationCube.gameObject.SetActive(true);
                           // worldAnchorRecalibrationButton.gameObject.SetActive(true);
                            //helpAnchorVisualizationCube.gameObject.SetActive(false);
                           // anchorLoaded = true;


                          //  calibrated = true;

                         //   if (OnSystemStarted != null) {
                          //      OnSystemStarted();
                         //   }
                        }
                      //  else {
                       //     StartCoroutine(startCalibration());
                       //     calibration_launched = true;
                       // }
                    }
#endif
    }

    private void isCalibrated() {
        if (checkedTopMarker && checkedCenterMarker && checkedRightMarker) {
            Debug.Log("Prepare for calibration" + this.position1);
            Calibrating();
            SetupAnchor();
        }
    }

    public void CheckMarkerRight() {
      //  rightMarker.GetComponent<TrackedImageManager>().enabled = false;
        checkedRightMarker = true;
        rightMarker.SetActive(false);
        Debug.Log("Position checked" + this.position3);
        isCalibrated();
    }

    public void CheckMarkerCenter() {
      //  centerMarker.GetComponent<TrackedImageManager>().enabled = false;
        centerMarker.SetActive(false);
        checkedCenterMarker = true;
        Debug.Log("Position checked" + this.position2);
        isCalibrated();
    }

    public void CheckMarkerTop() {
       // topMarker.GetComponent<TrackedImageManager>().enabled = false;
        topMarker.SetActive(false);
        checkedTopMarker = true;
        Debug.Log("Position checked" + this.position1);
        isCalibrated();
    }

    public void SetMarker1(GameObject gameObject) {
        this.position1 = gameObject.transform.position;
        Debug.Log("Position setted" + this.position1);
    }

    public void SetMarker2(GameObject gameObject) {
        this.position2 = gameObject.transform.position;
        Debug.Log("Position setted" + this.position2);
    }

    public void SetMarker3(GameObject gameObject) {
        this.position3 = gameObject.transform.position;
        Debug.Log("Position setted" + this.position3);
    }

}
