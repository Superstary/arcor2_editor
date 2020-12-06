using UnityEngine;
using UnityEngine.Events;

public class SceneTileHololens : TileHololens {
    public string ProjectId;
    public string SceneId;

    [SerializeField] private TMPro.TMP_Text sceneName, timestamp;

    public void InitTile(string name, UnityAction mainCallback, string sceneId,
        string sceneName) {
        base.InitTile(name, mainCallback);
        SceneId = sceneId;
    }

    public void SetTimestamp(string value) {
        timestamp.text = "Last modified: " + value;
    }
}
