using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using IO.Swagger.Model;
using RosSharp.Urdf;
using RuntimeGizmos;
using TMPro;
using UnityEngine;

namespace Base {
    public class RobotActionObject : ActionObject, IRobot {
        
        public TextMeshPro ActionObjectName;
        public GameObject RobotPlaceholderPrefab;

        private OutlineOnClick outlineOnClick;

        public bool ResourcesLoaded = false;

        [SerializeField]
        private GameObject EEOrigin;

        private bool eeVisible = false;

        public RobotModel RobotModel {
            get; private set;
        }
        public bool manipulationStarted {
            get;
            private set;
        }
        public bool updatePose {
            get;
            private set;
        }

        private List<RobotEE> EndEffectors = new List<RobotEE>();
        
        private GameObject RobotPlaceholder;

        private List<Renderer> robotRenderers = new List<Renderer>();
        private List<Collider> robotColliders = new List<Collider>();

        private bool transparent = false;

        private Shader standardShader;
        private Shader transparentShader;

        protected override void Start() {
            base.Start();
            SceneManager.Instance.OnShowRobotsEE += OnShowRobotsEE;
            SceneManager.Instance.OnHideRobotsEE += OnHideRobotsEE;
        }

        private async void OnDisable() {
            await DisableVisualisationOfEE();
            if (HasUrdf())
                await WebsocketManager.Instance.RegisterForRobotEvent(GetId(), false, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
            SceneManager.Instance.OnShowRobotsEE -= OnShowRobotsEE;
            SceneManager.Instance.OnHideRobotsEE -= OnHideRobotsEE;            
        }
        
        private void OnShowRobotsEE(object sender, EventArgs e) {
            _ = EnableVisualisationOfEE();            
        }

        private void OnHideRobotsEE(object sender, EventArgs e) {
            _ = DisableVisualisationOfEE();
        }

        protected override void Update() {
            if (manipulationStarted) {
                if (TransformGizmo.Instance.mainTargetRoot != null && GameObject.ReferenceEquals(TransformGizmo.Instance.mainTargetRoot.gameObject, gameObject)) {
                    if (!TransformGizmo.Instance.isTransforming && updatePose) {
                        updatePose = false;

                        if (ActionObjectMetadata.HasPose) {
                            UpdatePose();
                        } else {
                            PlayerPrefsHelper.SavePose("scene/" + SceneManager.Instance.SceneMeta.Id + "/action_object/" + Data.Id + "/pose",
                                transform.localPosition, transform.localRotation);
                        }
                    }

                    if (TransformGizmo.Instance.isTransforming)
                        updatePose = true;

                } else {
                    if (eeVisible)
                        ShowRobotEE();
                    manipulationStarted = false;
                }

            }

            base.Update();
        }

        private async void UpdatePose() {
            try {
                await WebsocketManager.Instance.UpdateActionObjectPose(Data.Id, GetPose());
            } catch (RequestFailedException e) {
                Notifications.Instance.ShowNotification("Failed to update action object pose", e.Message);
                ResetPosition();
            }
        }

        public void ShowRobotEE() {
            foreach (RobotEE ee in EndEffectors) {
                ee.gameObject.SetActive(true);
            }            
        }

        public void HideRobotEE() {
            foreach (RobotEE ee in EndEffectors) {
                try {
                    ee.gameObject.SetActive(false);
                } catch (MissingReferenceException) {
                    continue;
                }                    
            }            
        }

        public async Task DisableVisualisationOfEE() {
            eeVisible = false;
            if (EndEffectors.Count > 0) {
                await WebsocketManager.Instance.RegisterForRobotEvent(GetId(), false, RegisterForRobotEventRequestArgs.WhatEnum.Eefpose);
                HideRobotEE();
            }
        }
        

        public async Task EnableVisualisationOfEE() {
            eeVisible = true;
            if (!ResourcesLoaded)
                await LoadResources();
            if (EndEffectors.Count > 0) {
                await WebsocketManager.Instance.RegisterForRobotEvent(GetId(), true, RegisterForRobotEventRequestArgs.WhatEnum.Eefpose);
                ShowRobotEE();
            }
        }
        

        public async override void InitActionObject(string id, string type, Vector3 position, Quaternion orientation, string uuid, ActionObjectMetadata actionObjectMetadata, IO.Swagger.Model.CollisionModels customCollisionModels = null, bool loadResources = true) {
            base.InitActionObject(id, type, position, orientation, uuid, actionObjectMetadata);
            //UrdfManager.Instance.OnUrdfReady += OnUrdfDownloaded;
           
            // if there should be an urdf robot model
            if (ActionsManager.Instance.RobotsMeta.TryGetValue(type, out RobotMeta robotMeta) && !string.IsNullOrEmpty(robotMeta.UrdfPackageFilename)) {
                // check if robot model exists
                if (UrdfManager.Instance.CheckIfRobotModelExists(type)) {
                    // check if newer robot model exists on the server
                    if (UrdfManager.Instance.CheckIfNewerRobotModelExists(robotMeta.UrdfPackageFilename, type)) {
                        // TODO destroy the old robot version
                        UrdfManager.Instance.RemoveOldModels(type);

                        // download newer version of the urdf
                        UrdfManager.Instance.OnRobotUrdfModelLoaded += OnRobotModelLoaded;
                        StartCoroutine(UrdfManager.Instance.DownloadUrdfPackage(robotMeta.UrdfPackageFilename, robotMeta.Type));

                    } else {
                        // get the robot model
                        RobotModel = UrdfManager.Instance.GetRobotModelInstance(type);
                        if (RobotModel != null) {
                            RobotModelLoaded();
                        } else {
                            Debug.LogError("Fatal error, robot model should be present and loaded, but it is not. Report bug.");
                        }
                    }
                }
                // robot model is not loaded at all, check if urdf is downloaded locally and import it, otherwise download it
                else {
                    // check if newer robot model exists on the server.. if it is not downloaded at all, it will return true
                    if (UrdfManager.Instance.CheckIfNewerRobotModelExists(robotMeta.UrdfPackageFilename, type)) {
                        UrdfManager.Instance.OnRobotUrdfModelLoaded += OnRobotModelLoaded;
                        StartCoroutine(UrdfManager.Instance.DownloadUrdfPackage(robotMeta.UrdfPackageFilename, robotMeta.Type));
                    } else { // the robot zip is downloaded, thus start direct build
                        UrdfManager.Instance.OnRobotUrdfModelLoaded += OnRobotModelLoaded;
                        UrdfManager.Instance.BuildRobotModelFromUrdf(type);
                    }
                }
            }
            //LoadResources = loadResources;
            ResourcesLoaded = false;
        }

        private void OnRobotModelLoaded(object sender, RobotUrdfModelArgs args) {
            Debug.Log("URDF: robot is fully loaded");

            // check if the robot of the type we need was loaded
            if (args.RobotType == Data.Type) {
                // if so, lets ask UrdfManager for the robot model
                RobotModel = UrdfManager.Instance.GetRobotModelInstance(Data.Type);
               
                RobotModelLoaded();
                
                // if robot is loaded, unsubscribe from UrdfManager event
                UrdfManager.Instance.OnRobotUrdfModelLoaded -= OnRobotModelLoaded;
            }
        }

        private async void RobotModelLoaded() {
            Debug.Log("URDF: robot is fully loaded");

            RobotModel.RobotModelGameObject.transform.parent = transform;
            RobotModel.RobotModelGameObject.transform.localPosition = Vector3.zero;
            RobotModel.RobotModelGameObject.transform.localEulerAngles = Vector3.zero;

            // retarget OnClickCollider target to receive OnClick events
            foreach (OnClickCollider onCLick in RobotModel.RobotModelGameObject.GetComponentsInChildren<OnClickCollider>(true)) {
                onCLick.Target = gameObject;
            }

            RobotModel.SetActiveAllVisuals(true);

            outlineOnClick.ClearRenderers();
            RobotPlaceholder.SetActive(false);
            Destroy(RobotPlaceholder);

            robotColliders.Clear();
            robotRenderers.Clear();
            robotRenderers.AddRange(RobotModel.RobotModelGameObject.GetComponentsInChildren<Renderer>());
            robotColliders.AddRange(RobotModel.RobotModelGameObject.GetComponentsInChildren<Collider>());
            outlineOnClick.InitRenderers(robotRenderers);
            outlineOnClick.OutlineShaderType = OutlineOnClick.OutlineType.TwoPassShader;
            outlineOnClick.InitGizmoMaterials();
            await WebsocketManager.Instance.RegisterForRobotEvent(GetId(), true, RegisterForRobotEventRequestArgs.WhatEnum.Joints);
        }


        public override Vector3 GetScenePosition() {
            return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Pose.Position));
        }

        public override void SetScenePosition(Vector3 position) {
            Data.Pose.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
        }

        public override Quaternion GetSceneOrientation() {
            return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Pose.Orientation));
        }

        public override void SetSceneOrientation(Quaternion orientation) {
            Data.Pose.Orientation = DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation));
        }

        public override void Show() {
            foreach (Renderer renderer in robotRenderers) {
                renderer.enabled = true;
            }
        }

        public override void Hide() {
            foreach (Renderer renderer in robotRenderers) {
                renderer.enabled = false;
            }
        }

        public override void SetInteractivity(bool interactive) {
            foreach (Collider collider in robotColliders) {
                collider.enabled = interactive;
            }
        }

        public override void SetVisibility(float value) {
            base.SetVisibility(value);

            if (standardShader == null) {
                standardShader = Shader.Find("Standard");
            }

            if (transparentShader == null) {
                transparentShader = Shader.Find("Transparent/Diffuse");
            }

            // Set opaque shader
            if (value >= 1) {
                transparent = false;
                foreach (Renderer renderer in robotRenderers) {
                    // Robot has its outline active, we need to select second material,
                    // (first is mask, second is object material, third is outline)
                    if (renderer.materials.Length == 3) {
                        renderer.materials[1].shader = standardShader;
                    } else {
                        renderer.material.shader = standardShader;
                    }
                }
            }
            // Set transparent shader
            else {
                if (!transparent) {
                    foreach (Renderer renderer in robotRenderers) {
                        if (renderer.materials.Length == 3) {
                            renderer.materials[1].shader = transparentShader;
                        } else {
                            renderer.material.shader = transparentShader;
                        }
                    }
                    transparent = true;
                }
                // set alpha of the material
                foreach (Renderer renderer in robotRenderers) {
                    Material mat;
                    if (renderer.materials.Length == 3) {
                        mat = renderer.materials[1];
                    } else {
                        mat = renderer.material;
                    }
                    Color color = mat.color;
                    color.a = value;
                    mat.color = color;
                }
            }
        }

        public override void OnClick(Click type) {
            if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingActionObject ||
             GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.SelectingActionPointParent) {
                GameManager.Instance.ObjectSelected(this);
                return;
            }
            if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal) {
                return;
            }
            if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.SceneEditor &&
                GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor) {
                Notifications.Instance.ShowNotification("Not allowed", "Editation of action object only allowed in scene or project editor");
                return;
            }
           
            // HANDLE MOUSE
            if (type == Click.MOUSE_LEFT_BUTTON || type == Click.LONG_TOUCH) {
                // We have clicked with left mouse and started manipulation with object
                if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.SceneEditor) {
                    manipulationStarted = true;
                    HideRobotEE();
                    TransformGizmo.Instance.AddTarget(transform);
                    outlineOnClick.GizmoHighlight();
                }
            } else if (type == Click.MOUSE_RIGHT_BUTTON || type == Click.TOUCH) {
                ShowMenu();
                TransformGizmo.Instance.ClearTargets();
                outlineOnClick.GizmoUnHighlight();
            }
        }

        public async Task<List<string>> GetEndEffectorIds() {
            if (!ResourcesLoaded) {
                await LoadResources();
            }
            List<string> result = new List<string>();
            foreach (RobotEE ee in EndEffectors)
                result.Add(ee.EEId);
            return result;
        }

        public async Task<List<RobotEE>> GetEndEffectors() {
            if (!ResourcesLoaded) {
                await LoadEndEffectors();
            }
            return EndEffectors;            
        }

        private async Task LoadResources() {
            if (!ResourcesLoaded) {
                await LoadEndEffectors();
            }
            ResourcesLoaded = true;
        }

        public async Task LoadEndEffectors() {
            GameManager.Instance.ShowLoadingScreen("Loading end effectors of robot " + Data.Name);
            List<string> endEffectors = await WebsocketManager.Instance.GetEndEffectors(Data.Id);
            foreach (string eeId in endEffectors) {
                RobotEE ee = Instantiate(SceneManager.Instance.RobotEEPrefab, EEOrigin.transform).GetComponent<RobotEE>();
                ee.InitEE(this, eeId);
                ee.gameObject.SetActive(false);
                EndEffectors.Add(ee);
            }
            GameManager.Instance.HideLoadingScreen();
        }

        public override void CreateModel(CollisionModels customCollisionModels = null) {
            RobotPlaceholder = Instantiate(RobotPlaceholderPrefab, transform);
            RobotPlaceholder.transform.parent = transform;
            RobotPlaceholder.transform.localPosition = Vector3.zero;
            RobotPlaceholder.transform.localPosition = Vector3.zero;
            //Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);

            RobotPlaceholder.GetComponent<OnClickCollider>().Target = gameObject;

            robotColliders.Clear();
            robotRenderers.Clear();
            robotRenderers.AddRange(RobotPlaceholder.GetComponentsInChildren<Renderer>());
            robotColliders.AddRange(RobotPlaceholder.GetComponentsInChildren<Collider>());
            outlineOnClick = gameObject.GetComponent<OutlineOnClick>();
            outlineOnClick.InitRenderers(robotRenderers);
            outlineOnClick.OutlineShaderType = OutlineOnClick.OutlineType.OnePassShader;
            outlineOnClick.InitGizmoMaterials();
        }

        public override GameObject GetModelCopy() {
            throw new System.NotImplementedException();
        }


        public bool HasUrdf() {
            if (Base.ActionsManager.Instance.RobotsMeta.TryGetValue(Data.Type, out RobotMeta robotMeta)) {
                return !string.IsNullOrEmpty(robotMeta.UrdfPackageFilename);
            }
            return false;
        }

        public override void OnHoverStart() {
            if (!enabled)
                return;
            if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal &&
                GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.SelectingActionObject &&
                GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.SelectingActionPointParent) {
                if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.Closed) {
                    if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.PackageRunning)
                        return;
                } else {
                    return;
                }
            }
            if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.SceneEditor &&
                GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor &&
                GameManager.Instance.GetGameState() != GameManager.GameStateEnum.PackageRunning) {
                return;
            }
            ActionObjectName.gameObject.SetActive(true);
            outlineOnClick.Highlight();
        }

        public override void OnHoverEnd() {
            ActionObjectName.gameObject.SetActive(false);
            outlineOnClick.UnHighlight();
        }

        public override void UpdateUserId(string newUserId) {
            base.UpdateUserId(newUserId);
            ActionObjectName.text = newUserId;
        }

        public override void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger, bool visibility, bool interactivity) {
            base.ActionObjectUpdate(actionObjectSwagger, visibility, interactivity);
            ActionObjectName.text = actionObjectSwagger.Name;
        }

        public RobotEE GetEE(string ee_id) {
            foreach (RobotEE ee in EndEffectors)
                if (ee.EEId == ee_id)
                    return ee;
            throw new ItemNotFoundException("End effector with ID " + ee_id + " not found for " + GetName());
        }

        public void SetJointValue(string name, float angle) {
            RobotModel?.SetJointAngle(name, angle);
        }

        private void OnDestroy() {
            // if RobotModel was present, lets return it to the UrdfManager robotModel pool
            if (RobotModel != null) {
                if (UrdfManager.Instance != null) {
                    UrdfManager.Instance.ReturnRobotModelInstace(RobotModel);
                }
            }
        }
    }
}
