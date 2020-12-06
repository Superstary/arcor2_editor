using System;
using System.Collections.Generic;
using Base;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class HandMenuManager : MonoBehaviour {
    [SerializeField] private GameObject ConnectionTab;
    [SerializeField] private GameObject SceneTab;
    [SerializeField] private GameObject ProjectTab;
    [SerializeField] private GameObject ConnectionContent;
    [SerializeField] private GameObject SceneContent;
    [SerializeField] private GameObject ProjectContent;

    public GameObject TilePrefab;
    public GameObject ScenesDynamicContent;
    public GameObject ProjectsDynamicContent;

    private enum State {
        Connection,
        Scene,
        Project
    }

    private List<SceneTile> sceneTiles = new List<SceneTile>();
    private List<ProjectTile> projectTiles = new List<ProjectTile>();
    private List<PackageTile> packageTiles = new List<PackageTile>();

    private void Start() {
        if (ConnectionTab != null && SceneTab != null && ProjectTab != null) {
            Interactable ConnectionTabButton = ConnectionTab.transform.GetComponent<Interactable>();
            print(ConnectionTabButton);
            ConnectionTabButton.OnClick.AddListener(() => {
                SwitchContentHandler(State.Connection);
            });
            Interactable SceneTabButton = SceneTab.transform.GetComponent<Interactable>();
            SceneTabButton.OnClick.AddListener(() => {
                SwitchContentHandler(State.Scene);
            });
            Interactable ProjectTabButton = ProjectTab.transform.GetComponent<Interactable>();
            ProjectTabButton.OnClick.AddListener(() => {
                SwitchContentHandler(State.Project);
            });
        }

        if (ConnectionContent != null && SceneContent != null && ProjectContent != null) {
            ConnectionContent.SetActive(true);
            SceneContent.SetActive(false);
            ProjectContent.SetActive(false);
        }


        GameManager.Instance.OnSceneListChanged += UpdateScenes;
        GameManager.Instance.OnProjectsListChanged += UpdateProjects;
    }


    public void UpdateProjects(object sender, EventArgs eventArgs) {
        foreach (Transform t in ProjectsDynamicContent.transform) {
            Destroy(t.gameObject);
        }

        foreach (IO.Swagger.Model.ListProjectsResponseData project in GameManager.Instance.Projects) {
            ProjectTileHololens tile = Instantiate(TilePrefab, ProjectsDynamicContent.transform)
                .GetComponent<ProjectTileHololens>();
            print("tile :" + tile);
            print("project :" + project);

            try {

                tile.InitTile(project.Name,
                    () => GameManager.Instance.OpenProject(project.Id),
                    project.SceneId,
                    project.Id,
                    "");
            } catch (ItemNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.SaveLogs("Failed to load scene name.");
            }
        }

    }


    public void UpdateScenes(object sender, EventArgs eventArgs) {
        foreach (Transform t in ScenesDynamicContent.transform) {
            Destroy(t.gameObject);
        }

        foreach (IO.Swagger.Model.ListScenesResponseData scene in GameManager.Instance.Scenes) {
            SceneTileHololens tile = Instantiate(TilePrefab, ScenesDynamicContent.transform)
                .GetComponent<SceneTileHololens>();
            bool starred = PlayerPrefsHelper.LoadBool("scene/" + scene.Id + "/starred", false);
            tile.InitTile(scene.Name,
                () => GameManager.Instance.OpenScene(scene.Id),
                scene.Id, scene.Name);
        }

    }


    private void SwitchContentHandler(State state) {

        ConnectionContent.SetActive(false);
        SceneContent.SetActive(false);
        ProjectContent.SetActive(false);
        switch (state) {
            case State.Connection:
                ConnectionContent.SetActive(true);
                break;
            case State.Scene:
                SceneContent.SetActive(true);
                break;
            case State.Project:
                ProjectContent.SetActive(true);
                break;
        }
    }

}
