using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ShowScriptEditor
{
	public partial class Form1 : Form
	{
		TreeNode _contextNode;
		string _currentFilePath;
		bool _dirty;

		readonly string[] c_ValidStopConditions = { "trigger", "timer", "gesture", "oscMessage" };
		readonly string[] c_ValidActions = { "loadScene", "showObject", "hideObject" };

		public Form1()
		{
			InitializeComponent();
		}

		void UpdateTitle()
		{
			Text = string.Format("Show Script Editor - {0}{1}", Path.GetFileName(_currentFilePath), _dirty ? "*" : "");
		}

		void SetDirty()
		{
			_dirty = true;
			UpdateTitle();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "JSON Show Scripts|*.json|All Files|*.*";
			dlg.CheckFileExists = true;

			if (dlg.ShowDialog() == DialogResult.OK)
			{
				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ShowConfig));
				FileStream fs = File.OpenRead(dlg.FileName);
				ShowConfig cfg = (ShowConfig)ser.ReadObject(fs);
				fs.Close();

				treeView1.BeginUpdate();
				treeView1.Nodes.Clear();

				foreach (EventGroup eg in cfg.eventGroups)
				{
					TreeNode tn = treeView1.Nodes.Add(eg.name);
					tn.Tag = eg;
					eg.treeNode = tn;
					

					if (eg.stopCondition != null)
					{
						TreeNode stopCondition = tn.Nodes.Add("Stop Condition");
						stopCondition.Tag = eg.stopCondition;
					}

					TreeNode eventsNode = tn.Nodes.Add("Events");
					foreach (Event evt in eg.events)
					{
						TreeNode en = eventsNode.Nodes.Add(string.Format("{0}|{1}", evt.action, evt.arg1));
						en.Tag = evt;
					}
				}

				treeView1.EndUpdate();

				_currentFilePath = dlg.FileName;
				_dirty = false;
				UpdateTitle();
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(_currentFilePath))
				doSaveAs();
			else
				doSave();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			doSaveAs();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		bool doSave()
		{
			// Build a show config from the tree
			ShowConfig sc = new ShowConfig();
			sc.eventGroups = new List<EventGroup>();
			foreach (TreeNode tn in treeView1.Nodes)
				sc.eventGroups.Add((EventGroup)tn.Tag);

			// Validate all the properties
			if (!ValidateShowConfig(sc))
				return false;

			// Serialize to json
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ShowConfig));
			MemoryStream ms = new MemoryStream();
			ser.WriteObject(ms, sc);

			// Write to the file
			File.WriteAllBytes(_currentFilePath, ms.ToArray());

			// Clear dirty flag
			_dirty = false;
			UpdateTitle();
			return true;
		}

		bool doSaveAs()
		{
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.FileName = _currentFilePath;
			dlg.Filter = "JSON Show Scripts|*.json|All Files|*.*";
			if (dlg.ShowDialog() == DialogResult.Cancel)
				return false;

			_currentFilePath = dlg.FileName;
			doSave();
			return true;
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (_dirty)
			{
				DialogResult dr = MessageBox.Show("There are unsaved changes, would you like to save them now?", "Save Changes", MessageBoxButtons.YesNoCancel);
				switch (dr)
				{
					case DialogResult.Yes:
						if (string.IsNullOrEmpty(_currentFilePath))
						{
							if (!doSaveAs())
								e.Cancel = true;
						}
						else if (!doSave())
							e.Cancel = true;
						break;
					case DialogResult.No:
						break;
					case DialogResult.Cancel:
						e.Cancel = true;
						break;
				}
			}
		}

		private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (treeView1.SelectedNode == null)
				propertyGrid1.SelectedObject = null;
			else
				propertyGrid1.SelectedObject = treeView1.SelectedNode.Tag;
		}

		private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == MouseButtons.Right && e.Node != null)
			{
				_contextNode = e.Node;
				ContextMenu menu = new ContextMenu();
				menu.MenuItems.Add("Add Event Group to End").Click += OnAddEventGroup;

				if (e.Node.Tag is EventGroup)
				{
					EventGroup eg = (EventGroup)e.Node.Tag;

					menu.MenuItems.Add("Create Event Group Before").Click += OnCreateEventGroupBefore;
					menu.MenuItems.Add("Create Event Group After").Click += OnCreateEventGroupAfter;

					if (eg.stopCondition == null)
					{
						menu.MenuItems.Add("-");
						MenuItem mi = menu.MenuItems.Add("Add Stop Condition");
						mi.Click += OnAddStopCondition;
					}
				}
				else if (e.Node.Text == "Events")
				{
					menu.MenuItems.Add("-");
					menu.MenuItems.Add("Add Event").Click += OnAddEvent;
				}


				menu.Show(treeView1, e.Location);
			}
		}

		private void OnAddEvent(object sender, EventArgs e)
		{
			TreeNode p = _contextNode.Parent;
			EventGroup eg = (EventGroup)p.Tag;
			Event evt = new Event() { action = "invalid", arg1 = "none" };
			eg.events.Add(evt);
			TreeNode en = _contextNode.Nodes.Add(string.Format("{0}|{1}", evt.action, evt.arg1));
			en.Tag = evt;
			evt.treeNode = en;
			SetDirty();
		}

		private void OnAddStopCondition(object sender, EventArgs e)
		{
			EventGroup eg = (EventGroup)_contextNode.Tag;
			eg.stopCondition = new StopCondition();
			TreeNode stopCondition = _contextNode.Nodes.Add("Stop Condition");
			stopCondition.Tag = eg.stopCondition;
			SetDirty();
		}

		void InsertEventGroup(int insertIndex)
		{
			TreeNode egn = treeView1.Nodes.Insert(insertIndex, "New Event Group");
			egn.Tag = new EventGroup() { name = "New Event Group", events = new List<Event>(), treeNode = egn };

			TreeNode eventsNode = egn.Nodes.Add("Events");
			SetDirty();
		}

		private void OnCreateEventGroupBefore(object sender, EventArgs e)
		{
			int insertIndex = treeView1.Nodes.IndexOf(_contextNode);
			InsertEventGroup(insertIndex);
		}

		private void OnCreateEventGroupAfter(object sender, EventArgs e)
		{
			int insertIndex = treeView1.Nodes.IndexOf(_contextNode);
			InsertEventGroup(insertIndex + 1);
		}

		private void OnAddEventGroup(object sender, EventArgs e)
		{
			InsertEventGroup(treeView1.Nodes.Count);
		}

		private void treeView1_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
		{
			if (e.Node.Name == "Events" || e.Node.Name == "Stop Condition")
				e.CancelEdit = true;
		}

		private void treeView1_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
		{

		}

		private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			SetDirty();
			if (propertyGrid1.SelectedObject is EventGroup)
			{
				EventGroup eg = (EventGroup)(propertyGrid1.SelectedObject);
				if (e.ChangedItem.Label == "name")
					eg.treeNode.Text = eg.name;
			}
			else if (propertyGrid1.SelectedObject is Event)
			{
				Event evt = (Event)propertyGrid1.SelectedObject;
				evt.treeNode.Text = string.Format("{0}|{1}", evt.action, evt.arg1);
			}
			else if (propertyGrid1.SelectedObject is StopCondition)
			{
			}
		}

		private void treeView1_MouseUp(object sender, MouseEventArgs e)
		{
			TreeViewHitTestInfo hti = treeView1.HitTest(e.Location);
			if (hti.Node == null && e.Button == MouseButtons.Right)
			{
				ContextMenu cm = new ContextMenu();
				cm.MenuItems.Add("Add Event Group").Click += OnAddEventGroup;
				cm.Show(treeView1, e.Location);
			}
		}

		bool ValidateShowConfig(ShowConfig sc)
		{
			HashSet<string> validStopConditions = new HashSet<string>(c_ValidStopConditions);
			HashSet<string> validActions = new HashSet<string>(c_ValidActions);

			HashSet<string> egNames = new HashSet<string>();
			foreach (EventGroup eg in sc.eventGroups)
			{
				if (egNames.Contains(eg.name))
				{
					MessageBox.Show("Script contains multiple Event Groups named: \n\t" + eg.name, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
				egNames.Add(eg.name);

				if (eg.stopCondition != null)
				{
					if (!validStopConditions.Contains(eg.stopCondition.type))
					{
						string err = string.Format("Script contains invalid stop condition type: \n\t{0}\nEvent Group: {1}", eg.stopCondition.type, eg.name);
						MessageBox.Show(err, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}
				}

				foreach (Event evt in eg.events)
				{
					if (!validActions.Contains(evt.action))
					{
						string err = string.Format("Script contains invalid action:\n\t{0}\nEvent Group: {1}", evt.action, eg.name);
						MessageBox.Show(err, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return false;
					}
				}
			}


			return true;
		}
	}
}
