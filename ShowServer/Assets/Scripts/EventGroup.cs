using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Serializable]
public class StopCondition
{
	public string type;
	public string subtype;
	public string arg1;
}

[Serializable]
public class Event
{
	public string action;
	public string arg1;
}

[Serializable]
public class EventGroup
{
	public string name;
	public StopCondition stopCondition;
	public List<Event> events;
}

[Serializable]
public class ShowConfig
{
	public List<EventGroup> eventGroups;
	public int currentEventGroupIndex;
}