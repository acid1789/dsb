using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySharedLib;

public class ClientShowManager : MonoBehaviour
{

	string _serverAddress;
	ClientConfig _clientConfig;

	AsyncOperation _pendingSceneLoad;

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
		if (_pendingSceneLoad != null && _pendingSceneLoad.isDone)
		{
			_pendingSceneLoad = null;
		}
		
		if( _pendingSceneLoad == null )
			OSCManager.Update();	// Only process packets if pending scene loads are all done
	}
	
	bool OnServerStatus(OSCMessage msg)
	{
		if (_serverAddress == null)
		{
			_serverAddress = msg.From;
			OSCManager.SendTo(new OSCMessage("/unity/client/show/join", 0), msg.From);
		}
		return true;
	}

	bool OnLoadScene(OSCMessage msg)
	{
		if (msg.Args == null || msg.Args.Length < 1)
			Debug.LogError("Load scene message received with no argument!");
		else
		{
			string sceneToLoad = (string)msg.Args[0];
			Debug.Log("Loading scene: " + sceneToLoad);
			_pendingSceneLoad = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
			return false;
		}
		return true;
	}

	bool OnShowHideObject(OSCMessage msg)
	{
		if (msg.Args == null || msg.Args.Length < 1)
			Debug.LogError("showObject/hideObject message received with no argument!");
		else
		{
			string theObject = (string)msg.Args[0];
			bool show = msg.Address.EndsWith("showObject");
			Debug.LogFormat("{0} object: {1}", show ? "Showing" : "Hiding", theObject);

			GameObject obj = null;
			ShowObject[] objects = Resources.FindObjectsOfTypeAll<ShowObject>();
			foreach (ShowObject so in objects)
			{
				if (so.gameObject.name == theObject)
				{
					obj = so.gameObject;
					break;
				}
			}
			if (obj == null)
				Debug.LogErrorFormat("Failed to find object {0} in scene", theObject);
			else
				obj.SetActive(show);
		}
		return true;
	}
}
