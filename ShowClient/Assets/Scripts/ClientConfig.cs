using System;

[Serializable]
public class ClientConfig
{
	public string id;


	public int Id { get { int value = -1; int.TryParse(id, out value); return value; } }
	
}
