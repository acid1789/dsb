using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnityLauncher
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Show the system tray icon.
			using (ProcessIcon pi = new ProcessIcon())
			{
				pi.Display();
				StartMonitor();

				// Make sure the application runs!
				Application.Run();
			}
		}

		static ProcessMonitor s_Monitor;

		public static void StartMonitor()
		{
			string processName = Properties.Settings.Default.UnityProcess;
			if (s_Monitor != null && s_Monitor.ProcessName != processName)
			{
				s_Monitor.Kill();
				s_Monitor = null;
			}

			if (!string.IsNullOrEmpty(processName))
			{
				s_Monitor = new ProcessMonitor(processName);
			}				
		}
	}
}
