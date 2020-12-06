using UnityEngine;
using UnityEngine.UI;

public class LandingScreenHololens : Base.Singleton<LandingScreenHololens> {

    public GameObject Panel;
    public InputField Domain, Port;
    public GameObject ConnButton;
    public GameObject DisconnButton;
    private void Start() {
        Debug.Assert(Domain != null);
        Debug.Assert(Port != null);
        Domain = Domain.GetComponent<InputField>();
       // Domain.text =  "192.168.0.9";

        Port = Port.GetComponent<InputField>();
        Port.text = "6789";
    }

    public void ConnectToServer() {
        Debug.Log("Connecting ...");

        string domain = Domain.text;
        int port = int.Parse(Port.text);
        PlayerPrefs.SetString("arserver_domain", domain);
        PlayerPrefs.SetInt("arserver_port", port);

        PlayerPrefs.Save();
        Base.GameManager.Instance.ConnectToSever(domain, port);
        //Base.WebsocketManager.Instance.ConnectToServer(domain, port);
        Panel.gameObject.SetActive(false);
        if (DisconnButton != null && ConnButton != null) {
            DisconnButton.gameObject.SetActive(true);
            ConnButton.gameObject.SetActive(false);
        }
    }

    public void DisconnectFromServer() {
        Debug.Log("Disconnecting ...");
        Base.WebsocketManager.Instance.DisconnectFromSever();
        Panel.gameObject.SetActive(true);
        if (DisconnButton != null && ConnButton != null) {
            DisconnButton.gameObject.SetActive(false);
            ConnButton.gameObject.SetActive(true);
        }
    }

}
