using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnitySharedLib;

namespace UnityMonitor
{
	class ProcessMonitor
	{
		Thread _monitorThread;
		string _processName;

		bool _restartRequested;

		public ProcessMonitor(string processName)
		{
			OSCManager.Initialize("255.255.255.255", 19876);
			OSCManager.ListenToAddress("/unity/client/restart", OnRestartClient);

			_processName = processName;
			_monitorThread = new Thread(new ThreadStart(MonitorThreadProc)) { Name = "Monitor Thread" };
			_monitorThread.Start();
		}

		public void Kill()
		{
			OSCManager.Shutdown();
			_monitorThread.Abort();
		}

		public void MonitorThreadProc()
		{
			while (true)
			{
				OSCManager.Update();

				Process[] processes = Process.GetProcessesByName(_processName);
				if (processes != null && processes.Length > 0)
				{
					foreach (Process p in processes)
					{
						if (!p.Responding || _restartRequested)
							p.Kill();
					}
				}
				else
				{
					// Didn't find the process, start it now
					Process.Start(_processName);
				}
				_restartRequested = false;				

				Thread.Sleep(50);
			}
		}

		public bool OnRestartClient(OSCMessage msg)
		{
			_restartRequested = true;
			OSCManager.SendTo(new OSCMessage("/unity/client/restarting"), msg.From, 9876);
			return true;
		}
	}
}
