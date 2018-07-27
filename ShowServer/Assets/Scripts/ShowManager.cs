using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnitySharedLib;

public class ShowManager : MonoBehaviour
{

	public string JSON_URL;
	public Text ShowStep_Name;
	public Text ShowStep_StopCondition;
	public Text ShowStep_StopConditionSecondary;


	ShowConfig _theShow;
	int _lastSceneMarker;
	string _waitingForTrigger;
	string _waitingForGesture;
	double _timerSeconds;
	System.DateTime _timerMarker;

	System.DateTime _lastStatusTime;

	// Use this for initialization
	void Start()
	{
		SharedLogger.ListenToMessages(LogMessageHandler);
		OSCManager.Initialize();
		OSCManager.ListenToAddress("/unity/client/show/join", OnJoinShow);

		if (Debug.isDebugBuild && File.Exists("show_debug.json"))
			LoadJSON(File.ReadAllText("show_debug.json"));
		else if (!string.IsNullOrEmpty(JSON_URL))
		{
			StartCoroutine(FetchShowScript());
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

		if (_theShow != null)
		{
			if ((System.DateTime.Now - _lastStatusTime).TotalSeconds > 5)
			{
				OSCManager.SendToAll(new OSCMessage("/unity/server/status", null));
				_lastStatusTime = System.DateTime.Now;
			}

			if (_timerSeconds > 0)
			{
				System.TimeSpan ts = (System.DateTime.Now - _timerMarker);
				if (ts.TotalSeconds >= _timerSeconds)
				{
					// Timer expired, go to the next step
					Show_ExecuteStep();
				}
				else
				{
					SetSecondaryShowText((_timerSeconds - ts.TotalSeconds).ToString("N3"));
				}
			}

			// Bypass gesture with space bar
			if (_waitingForGesture != null)
			{
				if (Input.GetKeyDown(KeyCode.Space))
					Show_ExecuteStep();
			}
		}
	}

	void SetSecondaryShowText(string text)
	{
		if (ShowStep_StopConditionSecondary != null)
			ShowStep_StopConditionSecondary.text = text;
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
		_theShow = JsonUtility.FromJson<ShowConfig>(json);
		_lastSceneMarker = 0;
		Show_ExecuteStep();
	}

	void Show_ExecuteStep()
	{
		// Reset stop conditions
		_timerSeconds = -1;
		_waitingForGesture = null;
		_waitingForTrigger = null;

		// Grab the next step
		EventGroup step = _theShow.eventGroups[_theShow.currentEventGroupIndex++];
		string stepName = string.Format("([0]){1}", _theShow.currentEventGroupIndex, step.name);
		Debug.Log("Executing step: " + stepName);
		if (ShowStep_Name != null)
			ShowStep_Name.text = stepName;

		// Execute the event actions
		foreach (Event evt in step.events)
		{
			if (!Show_DoEvent(evt))
				return;	// Event wants to not continue this script path.
		}

		// If this step doesn't have a stop condition, execute the next step
		if (step.stopCondition == null)
			Show_ExecuteStep();
		else
		{
			if (ShowStep_StopCondition != null)
				ShowStep_StopCondition.text = step.stopCondition.type;

			switch (step.stopCondition.type)
			{
				case "trigger":
					_waitingForTrigger = step.stopCondition.arg1;
					SetSecondaryShowText(_waitingForTrigger);
					break;
				case "timer":
					_timerSeconds = ParseTimer(step.stopCondition.arg1);
					_timerMarker = System.DateTime.Now;
					break;
				case "gesture":
					_waitingForGesture = step.stopCondition.arg1;
					SetSecondaryShowText(_waitingForGesture);
					break;
				default:
					Debug.LogError("Unknown stop condition type: " + step.stopCondition.type);
					break;
			}
		}
	}

	bool Show_DoEvent(Event evt)
	{
		switch (evt.action)
		{
			case "loadScene":
				Debug.Log("SHOW - Loading scene: " + evt.arg1);
				_lastSceneMarker = _theShow.currentEventGroupIndex - 1;
				OSCManager.SendToAll(new OSCMessage("/unity/server/show/loadScene", evt.arg1));				
				break;
			case "showObject":
				Debug.Log("SHOW - showObject: " + evt.arg1);
				OSCManager.SendToAll(new OSCMessage("/unity/server/show/showObject", evt.arg1));
				break;
			case "hideObject":
				Debug.Log("SHOW - hideObject: " + evt.arg1);
				OSCManager.SendToAll(new OSCMessage("/unity/server/show/hideObject", evt.arg1));
				break;
			case "goto":
				Debug.Log("SHOW - goto: " + evt.arg1);
				for (int i = 0; i < _theShow.eventGroups.Count; i++)
				{
					if (_theShow.eventGroups[i].name == evt.arg1)
					{
						_theShow.currentEventGroupIndex = i;
						Show_ExecuteStep();
						return false;
					}
				}
				break;
			default:
				Debug.LogError("Unsupported event action encountered: " + evt.action);
				break;
		}
		return true;
	}

	double ParseTimer(string timerString)
	{
		double seconds = 0;
		string[] elements = timerString.Split(':');

		for (int i = elements.Length - 1; i >= 0; i--)
		{
			int val = 0;
			if (int.TryParse(elements[i], out val))
				seconds += ValueToSeconds((elements.Length - 1) - i, val);
		}

		return seconds;
	}

	double ValueToSeconds(int timerStep, int val)
	{
		switch (timerStep)
		{
			case 0: return val * 0.001;
			case 1: return val;
			case 2: return val * 60;
			case 3: return val * 60 * 60;
			default: return 0;
		}
	}

	public void OnTrigger(string trigger)
	{
		if (_waitingForTrigger != null && _waitingForTrigger == trigger)
			Show_ExecuteStep();
	}

	void OnJoinShow(OSCMessage msg)
	{
		int clientId = -1;
		if (msg.Args != null && msg.Args.Length > 0)
			clientId = (int)msg.Args[0];

		// This client is just joining right now, send them everything from the last scene load up until the current step
		for (int i = _lastSceneMarker; i < _theShow.currentEventGroupIndex; i++)
		{
			EventGroup eg = _theShow.eventGroups[i];
			foreach (Event evt in eg.events)
			{
				switch (evt.action)
				{
					case "loadScene":
						Debug.LogFormat("SHOW - Sending Loading scene: {0} to client: ({2}){1} ", evt.arg1, msg.From, clientId);
						_lastSceneMarker = _theShow.currentEventGroupIndex - 1;
						OSCManager.SendTo(new OSCMessage("/unity/server/show/loadScene", evt.arg1), msg.From);
						break;
					case "showObject":
						Debug.LogFormat("SHOW - Sending showObject: {0} to cleint: ({2}){1} ", evt.arg1, msg.From, clientId);
						OSCManager.SendTo(new OSCMessage("/unity/server/show/showObject", evt.arg1), msg.From);
						break;
					case "hideObject":
						Debug.LogFormat("SHOW - Sending hideObject: {0} to cleint: ({2}){1} ", evt.arg1, msg.From, clientId);
						OSCManager.SendTo(new OSCMessage("/unity/server/show/hideObject", evt.arg1), msg.From);
						break;
				}
			}
		}
		
	}

	void LogMessageHandler(SharedLogger.MessageType type, string message)
	{
		switch (type)
		{
			case SharedLogger.MessageType.Debug:
				if (Debug.isDebugBuild)
					Debug.Log(message);
				break;
			case SharedLogger.MessageType.Info:
				Debug.Log(message);
				break;
			case SharedLogger.MessageType.Warning:
				Debug.LogWarning(message);
				break;
			case SharedLogger.MessageType.Error:
				Debug.LogError(message);
				break;
		}
	}
}
