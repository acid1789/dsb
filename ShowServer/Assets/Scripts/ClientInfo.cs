using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class ClientInfo
{
	public int ClientID;
	public string IPAddress;
	public DateTime LastSeenTime;
	public DateTime RestartMsgTime;

	public ClientInfo(int clientId, string ipAddress)
	{
		ClientID = clientId;
		IPAddress = ipAddress;
	}

	public void MarkLastSeen()
	{
		LastSeenTime = DateTime.Now;
	}
}
