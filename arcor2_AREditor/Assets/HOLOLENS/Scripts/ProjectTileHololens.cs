using UnityEngine;
using UnityEngine.Events;

public class ProjectTileHololens : TileHololens {
    public string ProjectId;
    public string SceneId;

    [SerializeField] private TMPro.TMP_Text sceneName, timestamp;

    public void InitTile(string name, UnityAction mainCallback, string sceneId, string projectId,
        string sceneName) {
        base.InitTile(name, mainCallback);
        SceneId = sceneId;
        ProjectId = projectId;

    }

    public void SetTimestamp(string value) {
        timestamp.text = "Last modified: " + value;
    }
}
