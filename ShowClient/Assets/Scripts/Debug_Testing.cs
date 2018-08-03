using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_Testing : MonoBehaviour {
	

	// Use this for initialization
	void Start () {

		gameObject.SetActive(Debug.isDebugBuild);
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void OnSimulateLockUp()
	{
		while (true)
		{
			Debug.Log("Simulated Client Lock");
		}
	}

	public void OnSimulateCrash()
	{
		Application.Quit();
	}
}
