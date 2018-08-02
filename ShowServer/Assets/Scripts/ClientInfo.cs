using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class ClientInfo
{
	public enum ConnectionStatus
	{
		Normal,
		NotResponding,
		Restarting
	}

	public int ClientID;
	public string IPAddress;
	public DateTime LastSeenTime;
	public DateTime RestartMsgTime;
	public ConnectionStatus Status;

	public ClientInfo(int clientId, string ipAddress)
	{
		ClientID = clientId;
		IPAddress = ipAddress;
		Status = ConnectionStatus.Normal;
	}

	public void MarkLastSeen()
	{
		LastSeenTime = DateTime.Now;
		Status = ConnectionStatus.Normal;
	}
}
