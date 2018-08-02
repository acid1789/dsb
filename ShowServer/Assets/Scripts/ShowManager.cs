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
	public int RemoteManagerPort = 19876;


	ShowConfig _theShow;
	int _lastSceneMarker;
	string _waitingForTrigger;
	string _waitingForGesture;
	string _waitingForOSCMessage;
	double _timerSeconds;
	System.DateTime _timerMarker;

	System.DateTime _lastStatusTime;
	Dictionary<int, ClientInfo> _knownClients;

	// Use this for initialization
	void Start()
	{
		_knownClients = new Dictionary<int, ClientInfo>();

		SharedLogger.ListenToMessages(LogMessageHandler);
		OSCManager.Initialize("239.1.2.3");
		OSCManager.ListenToAddress("/unity/client/show/join", OnJoinShow);
		OSCManager.ListenToAddress("/unity/client/status", OnClientStatus);
		OSCManager.ListenToAddress("/unity/client/restarting", OnClientRestarting);

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

		foreach (var kvp in _knownClients)
		{
			if( kvp.Value.Status == ClientInfo.ConnectionStatus.Normal &&
				((System.DateTime.Now - kvp.Value.LastSeenTime).TotalSeconds > 5))
			{
				// This client hasn't been seen for 5 seconds
				kvp.Value.Status = ClientInfo.ConnectionStatus.NotResponding;
			}

			if (kvp.Value.Status == ClientInfo.ConnectionStatus.NotResponding && 
				((System.DateTime.Now - kvp.Value.RestartMsgTime).TotalSeconds > 2) )
			{
				// Its been more than 2 seconds since the restarter was notified, send it again
				OSCManager.SendTo(new OSCMessage("/unity/client/restart"), kvp.Value.IPAddress, RemoteManagerPort);
				kvp.Value.RestartMsgTime = System.DateTime.Now;
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
				return; // Event wants to not continue this script path.
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
				case "oscMessage":
					_waitingForOSCMessage = step.stopCondition.arg1;
					SetSecondaryShowText(_waitingForOSCMessage);
					OSCManager.ListenToAddress(_waitingForOSCMessage, OnOSCMessage);
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

	bool OnJoinShow(OSCMessage msg)
	{
		int clientId = -1;
		if (msg.Args != null && msg.Args.Length > 0)
			clientId = (int)msg.Args[0];

		if (clientId >= 0)
		{
			if (!_knownClients.ContainsKey(clientId))
				_knownClients[clientId] = new ClientInfo(clientId, msg.From);
			_knownClients[clientId].MarkLastSeen();
			_knownClients[clientId].IPAddress = msg.From;
		}

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
		return true;
	}

	bool OnClientStatus(OSCMessage msg)
	{
		int clientId = -1;
		if (msg.Args != null && msg.Args.Length > 0)
			clientId = (int)msg.Args[0];

		if (clientId >= 0)
		{
			if (!_knownClients.ContainsKey(clientId))
				_knownClients[clientId] = new ClientInfo(clientId, msg.From);
			_knownClients[clientId].MarkLastSeen();
		}
		return true;
	}

	bool OnClientRestarting(OSCMessage msg)
	{
		foreach (var kvp in _knownClients)
		{
			if (kvp.Value.IPAddress == msg.From)
			{
				kvp.Value.Status = ClientInfo.ConnectionStatus.Restarting;
				break;
			}
		}		
		return true;
	}

	bool OnOSCMessage(OSCMessage msg)
	{
		OSCManager.ForgetAddress(_waitingForOSCMessage);
		Show_ExecuteStep();
		return true;
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