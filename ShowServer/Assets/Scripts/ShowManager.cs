using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnitySharedLib;

public class ShowManager : MonoBehaviour {

	public string JSON_URL;

	ShowConfig _config;
	int _currentStep;

	System.DateTime _lastStatusTime;

	// Use this for initialization
	void Start () {
		OSCManager.Initialize();

		_currentStep = -1;
		if (Debug.isDebugBuild && File.Exists("show_debug.json"))
			LoadJSON(File.ReadAllText("show_debug.json"));
		else if( !string.IsNullOrEmpty(JSON_URL) )
		{
			StartCoroutine(FetchShowScript());
		}

	}
	
	// Update is called once per frame
	void Update ()
	{
		OSCManager.Update();

		if (_config != null)
		{
			if ((System.DateTime.Now - _lastStatusTime).TotalSeconds > 5)
			{
				OSCManager.SendToAll(new OSCMessage("/unity/server/status", null));
				_lastStatusTime = System.DateTime.Now;
			}

		}		
	}

	IEnumerator FetchShowScript()
	{
		using (WWW www = new WWW(JSON_URL))
		{
			yield return www;
			LoadJSON(www.text);
		}
	}

	void LoadJSON(string json)
	{
		_config = JsonUtility.FromJson<ShowConfig>(json);
	}
}
