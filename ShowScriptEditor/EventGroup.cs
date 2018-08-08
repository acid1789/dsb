using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;

[DataContract]
public class StopCondition
{
	[DataMember]
	public string type { get; set; }
	[DataMember]
	public string subtype { get; set; }
	[DataMember]
	public string arg1 { get; set; }
}

[DataContract]
public class Event
{
	[DataMember]
	public string action { get; set; }
	[DataMember]
	public string arg1 { get; set; }

	public TreeNode treeNode;
}

[DataContract]
public class EventGroup
{
	[DataMember]
	public string name { get; set; }
	[DataMember]
	public StopCondition stopCondition;
	[DataMember]
	public List<Event> events;

	public TreeNode treeNode;
}

[DataContract]
public class ShowConfig
{
	[DataMember]
	public List<EventGroup> eventGroups;
}