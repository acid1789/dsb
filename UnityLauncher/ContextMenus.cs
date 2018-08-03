using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace UnityLauncher
{
	class ContextMenus
	{
		/// <summary>
		/// Creates this instance.
		/// </summary>
		/// <returns>ContextMenuStrip</returns>
		public ContextMenuStrip Create()
		{
			// Add the default menu options.
			ContextMenuStrip menu = new ContextMenuStrip();
			ToolStripMenuItem item;

			item = new ToolStripMenuItem();
			item.Text = "Choose Unity Application";
			item.Click += ChooseUnity_Click;
			menu.Items.Add(item);
			
			// Exit.
			item = new ToolStripMenuItem();
			item.Text = "Exit";
			item.Click += new System.EventHandler(Exit_Click);
			menu.Items.Add(item);

			return menu;
		}

		private void ChooseUnity_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Browse for Unity Application";
			ofd.Filter = "Applications|*.exe";
			ofd.FileName = Properties.Settings.Default.UnityProcess;
			ofd.CheckFileExists = true;
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				Properties.Settings.Default.UnityProcess = ofd.FileName;
				Properties.Settings.Default.Save();

				Program.StartMonitor();
			}
		}

		/// <summary>
		/// Processes a menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void Exit_Click(object sender, EventArgs e)
		{
			// Quit without further ado.
			Application.Exit();
		}
	}
}
