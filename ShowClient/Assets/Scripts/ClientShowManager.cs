using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySharedLib;

public class ClientShowManager : MonoBehaviour {

	string _serverAddress;
	System.DateTime _lastServerSearchTime;
	ClientConfig _clientConfig;

	// Use this for initialization
	void Start ()
	{
		OSCManager.Initialize();
		OSCManager.ListenToAddress("/unity/server/status", OnServerStatus);
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
	void Update ()
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
}
