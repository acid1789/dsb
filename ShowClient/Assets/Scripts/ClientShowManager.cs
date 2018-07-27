using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySharedLib;

public class ClientShowManager : MonoBehaviour
{

	string _serverAddress;
	System.DateTime _lastServerSearchTime;
	ClientConfig _clientConfig;

	// Use this for initialization
	void Start()
	{
		DontDestroyOnLoad(gameObject);
		OSCManager.Initialize();
		OSCManager.ListenToAddress("/unity/server/status", OnServerStatus);
		OSCManager.ListenToAddress("/unity/server/show/loadScene", OnLoadScene);
		OSCManager.ListenToAddress("/unity/server/show/showObject", OnShowHideObject);
		OSCManager.ListenToAddress("/unity/server/show/hideObject", OnShowHideObject);
		try
		{
			_clientConfig = JsonUtility.FromJson<ClientConfig>(System.IO.File.ReadAllText("client_config.json"));
		}
		catch (System.Exception ex)
		{
			Debug.LogError(ex.ToString());
		}
	}

	private void OnApplicationQuit()
	{
		OSCManager.Shutdown();
	}

	// Update is called once per frame
	void Update()
	{
		OSCManager.Update();

		if (string.IsNullOrEmpty(_serverAddress))
		{
			UpdateFindServer();
		}
		else
		{
			UpdateShow();
		}
	}

	void UpdateFindServer()
	{
		if ((System.DateTime.Now - _lastServerSearchTime).TotalSeconds > 2.5)
		{
			//OSCManager.SendToAll(new OSCMessage("/unity/client/findShow", (object)_clientConfig.id));
			_lastServerSearchTime = System.DateTime.Now;
		}
	}

	void UpdateShow()
	{
	}

	void OnServerStatus(OSCMessage msg)
	{
		_serverAddress = msg.From;
	}

	void OnLoadScene(OSCMessage msg)
	{
		if (msg.Args == null || msg.Args.Length < 1)
			Debug.LogError("Load scene message received with no argument!");
		else
		{
			string sceneToLoad = (string)msg.Args[0];
			Debug.Log("Loading scene: " + sceneToLoad);
			SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
		}
	}

	void OnShowHideObject(OSCMessage msg)
	{
		if (msg.Args == null || msg.Args.Length < 1)
			Debug.LogError("showObject/hideObject message received with no argument!");
		else
		{
			string theObject = (string)msg.Args[0];
			bool show = msg.Address.EndsWith("showObject");
			Debug.LogFormat("{0} object: {1}", show ? "Showing" : "Hiding", theObject);

			GameObject obj = GameObject.Find(theObject);
			if (obj == null)
				Debug.LogErrorFormat("Failed to find object {0} in scene", theObject);
			else
				obj.SetActive(show);
		}
	}
}
