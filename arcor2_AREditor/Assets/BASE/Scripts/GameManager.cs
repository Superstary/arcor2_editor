using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

namespace Base {

    public class StringEventArgs : EventArgs {
        public string Data {
            get; set;
        }

        public StringEventArgs(string data) {
            Data = data;
        }
    }

    public class GameManager : Singleton<GameManager> {

        public delegate void StringEventHandler(object sender, StringEventArgs args);

        public event EventHandler OnSaveProject;
        public event EventHandler OnLoadProject;
        public event EventHandler OnRunProject;
        public event EventHandler OnStopProject;
        public event EventHandler OnPauseProject;
        public event EventHandler OnResumeProject;
        public event EventHandler OnProjectsListChanged;
        public event EventHandler OnSceneListChanged;
        public event StringEventHandler OnConnectedToServer;
        public event StringEventHandler OnConnectingToServer;
        public event EventHandler OnDisconnectedFromServer;

               

        public GameObject ActionObjects, Scene, SpawnPoint;
        public GameObject ConnectionPrefab, APConnectionPrefab, ActionPointPrefab, PuckPrefab, ButtonPrefab;
        public GameObject RobotPrefab, TesterPrefab, BoxPrefab, WorkspacePrefab, UnknownPrefab;
        private string loadedScene;
        private IO.Swagger.Model.Project newProject, currentProject = new IO.Swagger.Model.Project("", "JabloPCB", new List<IO.Swagger.Model.ProjectObject>(), "JabloPCB");
        private IO.Swagger.Model.Scene newScene;
        private bool sceneReady;

        public List<IO.Swagger.Model.IdDesc> Projects = new List<IO.Swagger.Model.IdDesc>(), Scenes = new List<IO.Swagger.Model.IdDesc>();

        public bool SceneInteractable = true;

        public enum ConnectionStatusEnum {
            Connected, Disconnected
        }

        private ConnectionStatusEnum connectionStatus;

        public ConnectionStatusEnum ConnectionStatus {
            get => connectionStatus; set {
                connectionStatus = value;
                OnConnectionStatusChanged(connectionStatus);
            }
        }

        private void Awake() {
        }

        private void Start() {
            loadedScene = "";
            sceneReady = false;
            ConnectionStatus = ConnectionStatusEnum.Disconnected;
        }

        // Update is called once per frame
        private void Update() {
            if (newScene != null && ActionsManager.Instance.ActionsReady)
                SceneUpdated(newScene);

        }

        public void UpdateScene() {
            Scene.GetComponent<Scene>().Data.Objects.Clear();
            foreach (ActionObject actionObject in ActionObjects.transform.GetComponentsInChildren<ActionObject>().ToList()) {
                Scene.GetComponent<Scene>().Data.Objects.Add(actionObject.Data);
            }
            WebsocketManager.Instance.UpdateScene(Scene.GetComponent<Scene>().Data);
        }
        
        private async void OnConnectionStatusChanged(ConnectionStatusEnum newState) {
            switch (newState) {
                case ConnectionStatusEnum.Connected:
                    OnConnectedToServer?.Invoke(this, new StringEventArgs(WebsocketManager.Instance.APIDomainWS));
                    Scenes = await WebsocketManager.Instance.LoadScenes();
                    OnSceneListChanged?.Invoke(this, EventArgs.Empty);
                    
                    Projects = await WebsocketManager.Instance.LoadProjects();
                    OnProjectsListChanged?.Invoke(this, EventArgs.Empty);
                    WebsocketManager.Instance.UpdateObjectTypes();
                    break;
                case ConnectionStatusEnum.Disconnected:
                    Scene.SetActive(false);
                    OnDisconnectedFromServer?.Invoke(this, EventArgs.Empty);
                    Projects = new List<IO.Swagger.Model.IdDesc>();
                    Scenes = new List<IO.Swagger.Model.IdDesc>();
                    break;
            }
        }

       
        public void ConnectToSever(string domain, int port) {
            OnConnectingToServer?.Invoke(this, new StringEventArgs(WebsocketManager.Instance.GetWSURI(domain, port)));
            Scene.SetActive(true);
            WebsocketManager.Instance.ConnectToServer(domain, port);

        }

        public void DisconnectFromSever() {
            WebsocketManager.Instance.DisconnectFromSever();
        }

        public GameObject SpawnActionObject(string type, bool updateScene = true, string id = "") {
            if (!ActionsManager.Instance.ActionObjectMetadata.TryGetValue(type, out ActionObjectMetadata aom)) {
                return null;
            }
            GameObject obj;
            switch (type) {
                case "Robot":
                case "KinaliRobot":
                    obj = Instantiate(RobotPrefab, ActionObjects.transform);
                    break;
                case "Box":
                    obj = Instantiate(BoxPrefab, ActionObjects.transform);
                    break;
                case "Tester":
                    obj = Instantiate(TesterPrefab, ActionObjects.transform);
                    break;
                case "Workspace":
                    obj = Instantiate(WorkspacePrefab, ActionObjects.transform);
                    break;
                default:
                    obj = Instantiate(UnknownPrefab, ActionObjects.transform);
                    break;
            }

            obj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            obj.transform.position = SpawnPoint.transform.position;
            obj.GetComponent<ActionObject>().Data.Type = type;
            if (id == "")
                obj.GetComponent<ActionObject>().Data.Id = GetFreeIOName(type);
            else
                obj.GetComponent<ActionObject>().Data.Id = id;
            obj.GetComponent<ActionObject>().SetScenePosition(transform.position);
            obj.GetComponent<ActionObject>().SetSceneOrientation(transform.rotation);


            obj.GetComponent<ActionObject>().ActionObjectMetadata = aom;
            if (updateScene)
                UpdateScene();
            return obj;
        }

        private string GetFreeIOName(string ioType) {
            int i = 1;
            bool hasFreeName;
            string freeName = ioType;
            do {
                hasFreeName = true;
                foreach (ActionObject io in ActionObjects.GetComponentsInChildren<ActionObject>()) {
                    if (io.Data.Id == freeName) {
                        hasFreeName = false;
                    }
                }
                if (!hasFreeName)
                    freeName = ioType + i++.ToString();
            } while (!hasFreeName);

            return freeName;
        }

        public GameObject SpawnPuck(string action_id, ActionPoint ap, ActionObject actionObject, bool generateData, bool updateProject = true, string puck_id = "") {
            if (!actionObject.ActionObjectMetadata.ActionsMetadata.TryGetValue(action_id, out ActionMetadata am)) {
                Debug.LogError("Action " + action_id + " not supported by action object " + ap.ActionObject.name);
                return null;
            }
            GameObject puck = Instantiate(PuckPrefab);
            puck.transform.SetParent(ap.transform.Find("Pucks"));
            puck.transform.position = ap.transform.position + new Vector3(0f, ap.GetComponent<ActionPoint>().PuckCounter++ * 0.8f + 1f, 0f);
            const string glyphs = "0123456789";
            string newId = puck_id;
            if (newId == "") {
                newId = action_id;
                for (int j = 0; j < 4; j++) {
                    newId += glyphs[UnityEngine.Random.Range(0, glyphs.Length)];
                }
            }
            puck.GetComponent<Action>().Init(newId, am, ap, actionObject, generateData, updateProject);

            puck.transform.localScale = new Vector3(1f, 1f, 1f);
            if (updateProject) {
                UpdateProject();
            }
            return puck;
        }

        public GameObject SpawnActionPoint(ActionObject actionObject, bool updateProject = true) {
            GameObject AP = Instantiate(ActionPointPrefab, actionObject.transform.Find("ActionPoints"));
            AP.transform.position = actionObject.transform.Find("ActionPoints").position + new Vector3(1f, 0f, 0f);
            AP.transform.localScale = new Vector3(1f, 1f, 1f);

            GameObject c = Instantiate(ConnectionPrefab);
            c.GetComponent<LineRenderer>().enabled = true;
            c.transform.SetParent(ConnectionManager.Instance.transform);
            c.GetComponent<Connection>().target[0] = actionObject.GetComponent<RectTransform>();
            c.GetComponent<Connection>().target[1] = AP.GetComponent<RectTransform>();
            AP.GetComponent<ActionPoint>().ConnectionToIO = c.GetComponent<Connection>();
            AP.GetComponent<ActionPoint>().SetActionObject(actionObject);
            AP.GetComponent<ActionPoint>().SetScenePosition(transform.position);
            AP.GetComponent<ActionPoint>().SetSceneOrientation(transform.rotation);
            if (updateProject)
                UpdateProject();
            return AP;
        }

        public void SceneUpdated(IO.Swagger.Model.Scene scene) {
            sceneReady = false;
            newScene = null;
            if (!ActionsManager.Instance.ActionsReady) {
                newScene = scene;
                return;
            }
            Scene.GetComponent<Scene>().Data = scene;
            Dictionary<string, ActionObject> actionObjects = new Dictionary<string, ActionObject>();
            if (loadedScene != scene.Id) {
                foreach (ActionObject ao in ActionObjects.transform.GetComponentsInChildren<ActionObject>()) {
                    Destroy(ao.gameObject);
                }
                loadedScene = scene.Id;
            } else {
                foreach (ActionObject ao in ActionObjects.transform.GetComponentsInChildren<ActionObject>()) {
                    actionObjects[ao.Data.Id] = ao;
                }
            }

            foreach (IO.Swagger.Model.SceneObject actionObject in scene.Objects) {
                if (actionObjects.TryGetValue(actionObject.Id, out ActionObject ao)) {
                    if (actionObject.Type != ao.Data.Type) {
                        // type has changed, what now? delete object and create a new one?
                        Destroy(ao.gameObject);
                        // TODO: create a new one with new type
                    }

                    ao.Data = actionObject;
                    ao.gameObject.transform.position = ao.GetScenePosition();
                    ao.gameObject.transform.rotation = DataHelper.OrientationToQuaternion(actionObject.Pose.Orientation);
                } else {
                    GameObject new_ao = SpawnActionObject(actionObject.Type, false, actionObject.Id);
                    new_ao.transform.localRotation = DataHelper.OrientationToQuaternion(actionObject.Pose.Orientation);
                    new_ao.GetComponent<ActionObject>().Data = actionObject;
                    new_ao.gameObject.transform.position = new_ao.GetComponent<ActionObject>().GetScenePosition();
                }
            }


            sceneReady = true;
            if (newProject != null) {
                ProjectUpdated(newProject);

            }


        }

        public void ProjectUpdated(IO.Swagger.Model.Project project) {
            if (project.SceneId != loadedScene || !sceneReady) {
                newProject = project;
                return;
            }

            currentProject = project;

            Dictionary<string, ActionObject> actionObjects = new Dictionary<string, ActionObject>();

            foreach (ActionObject ao in ActionObjects.transform.GetComponentsInChildren<ActionObject>()) {
                actionObjects[ao.Data.Id] = ao;
            }

            Dictionary<string, string> connections = new Dictionary<string, string>();

            foreach (IO.Swagger.Model.ProjectObject projectObject in currentProject.Objects) {
                if (actionObjects.TryGetValue(projectObject.Id, out ActionObject actionObject)) {

                    foreach (ActionPoint ap in actionObject.transform.GetComponentsInChildren<ActionPoint>()) {
                        ap.DeleteAP(false);
                    }
                    foreach (IO.Swagger.Model.ProjectActionPoint projectActionPoint in projectObject.ActionPoints) {
                        GameObject actionPoint = SpawnActionPoint(actionObject, false);
                        actionPoint.GetComponent<ActionPoint>().Data = DataHelper.ProjectActionPointToActionPoint(projectActionPoint);

                        actionPoint.transform.position = actionPoint.GetComponent<ActionPoint>().GetScenePosition();

                        foreach (IO.Swagger.Model.Action projectAction in projectActionPoint.Actions) {
                            string originalIOName = projectAction.Type.Split('/').First();
                            string action_type = projectAction.Type.Split('/').Last();
                            if (actionObjects.TryGetValue(originalIOName, out ActionObject originalActionObject)) {
                                GameObject action = SpawnPuck(action_type, actionPoint.GetComponent<ActionPoint>(), originalActionObject, false, false, projectAction.Id);
                                action.GetComponent<Action>().Data = projectAction;

                                foreach (IO.Swagger.Model.ActionParameter projectActionParameter in projectAction.Parameters) {
                                    if (action.GetComponent<Action>().Metadata.Parameters.TryGetValue(projectActionParameter.Id, out ActionParameterMetadata actionParameterMetadata)) {
                                        ActionParameter actionParameter = new ActionParameter {
                                            ActionParameterMetadata = actionParameterMetadata,
                                            Data = projectActionParameter
                                        };
                                        action.GetComponent<Action>().Parameters.Add(actionParameter.Data.Id, actionParameter);
                                    }
                                }

                                foreach (IO.Swagger.Model.ActionIO actionIO in projectAction.Inputs) {
                                    if (actionIO.Default != "start") {
                                        connections[projectAction.Id] = actionIO.Default;
                                    }
                                    action.GetComponentInChildren<PuckInput>().Data = actionIO;
                                }

                                foreach (IO.Swagger.Model.ActionIO actionIO in projectAction.Outputs) {
                                    action.GetComponentInChildren<PuckOutput>().Data = actionIO;
                                }

                            }

                        }
                    }


                } else {
                    //object not exist? 
                }

            }
            foreach (KeyValuePair<string, string> connection in connections) {
                PuckInput input = FindPuck(connection.Key).transform.GetComponentInChildren<PuckInput>();
                PuckOutput output = FindPuck(connection.Value).transform.GetComponentInChildren<PuckOutput>();
                GameObject c = Instantiate(ConnectionPrefab);
                c.transform.SetParent(ConnectionManager.Instance.transform);
                c.GetComponent<Connection>().target[0] = input.gameObject.GetComponent<RectTransform>();
                c.GetComponent<Connection>().target[1] = output.gameObject.GetComponent<RectTransform>();
                //input.GetComponentInParent<Action>().Data.
                input.Connection = c.GetComponent<Connection>();
                output.Connection = c.GetComponent<Connection>();
            }
        }

        public GameObject FindPuck(string id) {

            foreach (Action action in ActionObjects.GetComponentsInChildren<Action>()) {
                if (action.Data.Id == id)
                    return action.gameObject;
            }
            return new GameObject();
        }



        public void UpdateProject() {
            List<ActionObject> list = new List<ActionObject>();
            list.AddRange(ActionObjects.transform.GetComponentsInChildren<ActionObject>());
            currentProject.Objects.Clear();
            currentProject.SceneId = Scene.GetComponent<Scene>().Data.Id;
            foreach (ActionObject actionObject in ActionObjects.transform.GetComponentsInChildren<ActionObject>()) {
                IO.Swagger.Model.ProjectObject projectObject = DataHelper.SceneObjectToProjectObject(actionObject.Data);
                foreach (ActionPoint actionPoint in actionObject.ActionPoints.GetComponentsInChildren<ActionPoint>()) {
                    IO.Swagger.Model.ProjectActionPoint projectActionPoint = DataHelper.ActionPointToProjectActionPoint(actionPoint.Data);
                    foreach (Action action in actionPoint.GetComponentsInChildren<Action>()) {
                        IO.Swagger.Model.Action projectAction = action.Data;
                        projectAction.Parameters = new List<IO.Swagger.Model.ActionParameter>();
                        foreach (ActionParameter parameter in action.Parameters.Values) {
                            IO.Swagger.Model.ActionParameter projectParameter = parameter.Data;
                            projectAction.Parameters.Add(projectParameter);
                        }
                        projectAction.Inputs = new List<IO.Swagger.Model.ActionIO>();
                        projectAction.Outputs = new List<IO.Swagger.Model.ActionIO>();
                        foreach (InputOutput inputOutput in action.GetComponentsInChildren<InputOutput>()) {
                            if (inputOutput.GetType() == typeof(PuckInput)) {
                                projectAction.Inputs.Add(inputOutput.Data);
                            } else {
                                projectAction.Outputs.Add(inputOutput.Data);
                            }
                        }

                        projectActionPoint.Actions.Add(projectAction);
                    }
                    projectObject.ActionPoints.Add(projectActionPoint);
                }
                currentProject.Objects.Add(projectObject);
            }


            WebsocketManager.Instance.UpdateProject(currentProject);
        }
        public async Task LoadScenes() {
            Scenes = await WebsocketManager.Instance.LoadScenes();
            OnSceneListChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task LoadProjects() {
            Scenes = await WebsocketManager.Instance.LoadProjects();
            OnProjectsListChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task<IO.Swagger.Model.SaveSceneResponse> SaveScene() {
            IO.Swagger.Model.SaveSceneResponse response = await WebsocketManager.Instance.SaveScene();
            LoadScenes();
            return response;
        }

        public async Task<IO.Swagger.Model.SaveProjectResponse> SaveProject() {
            IO.Swagger.Model.SaveProjectResponse response = await WebsocketManager.Instance.SaveProject();
            LoadProjects();
            OnSaveProject?.Invoke(this, EventArgs.Empty);
            return response;
        }

        public void LoadProject(string id) {
            WebsocketManager.Instance.LoadProject(id);
            OnLoadProject?.Invoke(this, EventArgs.Empty);
        }

        public void RunProject() {
            WebsocketManager.Instance.RunProject(currentProject.Id);
            OnRunProject?.Invoke(this, EventArgs.Empty);
        }

        public void StopProject() {
            WebsocketManager.Instance.StopProject();
            OnStopProject?.Invoke(this, EventArgs.Empty);
        }

        public void PauseProject() {
            WebsocketManager.Instance.PauseProject();
            OnPauseProject?.Invoke(this, EventArgs.Empty);
        }


        public void ResumeProject() {
            WebsocketManager.Instance.ResumeProject();
            OnResumeProject?.Invoke(this, EventArgs.Empty);
        }


        public void CreateNewObjectType(IO.Swagger.Model.ObjectTypeMeta objectType) {
            WebsocketManager.Instance.CreateNewObjectType(objectType);
        }

        public void ExitApp() => Application.Quit();

        public void UpdateActionPointPosition(ActionPoint ap, string robotId, string endEffectorId) => WebsocketManager.Instance.UpdateActionPointPosition(ap.Data.Id, robotId, endEffectorId);
        public void UpdateActionObjectPosition(ActionObject ao, string robotId, string endEffectorId) => WebsocketManager.Instance.UpdateActionObjectPosition(ao.Data.Id, robotId, endEffectorId);

        public async Task<IO.Swagger.Model.FocusObjectStartResponse> StartObjectFocusing(string objectId, string robotId, string endEffector) {
            return await WebsocketManager.Instance.StartObjectFocusing(objectId, robotId, endEffector);
        }

        public void SavePosition(string objectId, int pointIdx) {
            WebsocketManager.Instance.SavePosition(objectId, pointIdx);
        }

        public void FocusObjectDone(string objectId) {
            WebsocketManager.Instance.FocusObjectDone(objectId);
        }

        public void NewProject(string name, string sceneId, string robotSystemId) {
            if (name == "" || robotSystemId == "") {
                return;
            }
            if (sceneId == null) {
                sceneId = name; // if no scene defined, create a new one with the name of the project
            } else {
                throw new NotImplementedException();
            }
            IO.Swagger.Model.Scene scene = new IO.Swagger.Model.Scene(id: sceneId, objects: new List<IO.Swagger.Model.SceneObject>(), robotSystemId: robotSystemId);
            IO.Swagger.Model.Project project = new IO.Swagger.Model.Project(id: name, objects: new List<IO.Swagger.Model.ProjectObject>(), sceneId: sceneId);
            WebsocketManager.Instance.UpdateScene(scene);
            SceneUpdated(scene);
            WebsocketManager.Instance.UpdateProject(project);
            ProjectUpdated(project);
        }



    }

}