using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace UnityMonitor
{
	public partial class Service1 : ServiceBase
	{
		ProcessMonitor _monitor;

		public Service1()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{

			string unityProcessName = Environment.GetEnvironmentVariable("UNITY_MONITOR_TARGET");
			_monitor = new ProcessMonitor(unityProcessName);
		}

		protected override void OnStop()
		{
			_monitor.Kill();
		}
	}
}
