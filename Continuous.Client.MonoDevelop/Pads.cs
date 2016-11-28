using System;
using System.Linq;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

namespace Continuous.Client.XamarinStudio
{
	public enum Pads
	{
		Main,
	}

	public class MainPad : PadContent
	{
		readonly MainPadControl control = new MainPadControl ();

		#region IPadContent implementation
		protected override void Initialize (IPadWindow window)
		{
			control.ShowAll ();
		}
		public void RedrawContent ()
		{
		}
		public override Control Control {
			get {
				return control;
			}
		}
		#endregion
		#region IDisposable implementation
		public override void Dispose ()
		{
			control.Dispose ();
		}
		#endregion
	}

	public class MainPadControl : VBox
	{
		protected ContinuousEnv Env { get { return ContinuousEnv.Shared; } }

		readonly VBox toolbar = new VBox ();
		readonly HBox toolbar0 = new HBox();
		readonly HBox toolbar1 = new HBox();
		readonly HBox toolbar2 = new HBox ();
		readonly Button runButton = new Button { Label = "Visualize Type" };
		readonly Button refreshButton = new Button { Label = "Refresh" };
		readonly Button stopButton = new Button { Label = "Stop" };
		readonly Button clearButton = new Button { Label = "Clear Edits" };
		readonly Label hostLabel = new Label { Text = "Device:" };
		readonly Entry hostEntry = new Entry { Text = ContinuousEnv.Shared.IP };
		//readonly Label portLabel = new Label { Text = "Port:" };
		//readonly Entry portEntry = new Entry { Text = ContinuousEnv.Shared.Port.ToString () };
		readonly NodeStore dependenciesStore = new NodeStore (typeof(DependencyTreeNode));
		readonly NodeView dependenciesView;
		readonly Label alertLabel = new Label {
			Justify = Justification.Left,
			Selectable = true
		};

		public MainPadControl ()
		{
			alertLabel.ModifyFg (StateType.Normal, new Gdk.Color(0xC0, 0x0, 0x0));

			Env.LinkedMonitoredCode += Env_LinkedMonitoredCode;
			Env.Alerted += Env_Alerted;

			runButton.Clicked += RunButton_Clicked;
			refreshButton.Clicked += RefreshButton_Clicked;
			stopButton.Clicked += StopButton_Clicked;
			clearButton.Clicked += ClearButton_Clicked;
			hostEntry.Changed += HostEntry_Changed;
			//portEntry.Changed += PortEntry_Changed;

			dependenciesView = new NodeView (dependenciesStore);
			dependenciesView.AppendColumn ("Dependency", new CellRendererText (), "text", 0);
			dependenciesView.AppendColumn ("Status", new CellRendererText (), "text", 1);

			toolbar0.PackStart (runButton, false, false, 4);
			toolbar0.PackStart (refreshButton, false, false, 4);
			toolbar0.PackStart (stopButton, false, false, 4);
			toolbar0.PackEnd (clearButton, false, false, 4);
			toolbar1.PackStart (hostLabel, false, false, 4);
			toolbar1.PackStart (hostEntry, false, false, 4);
			toolbar2.PackStart (alertLabel, false, false, 4);
			//toolbar1.PackStart (portLabel, false, false, 4);
			//toolbar1.PackStart (portEntry, false, false, 4);
			toolbar.PackStart (toolbar0, false, false, 0);
			toolbar.PackStart (toolbar1, false, false, 0);
			toolbar.PackStart (toolbar2, false, false, 0);
			PackStart (toolbar, false, false, 0);
			PackEnd (dependenciesView, true, true, 0);
		}

		void Env_LinkedMonitoredCode (LinkedCode obj)
		{
			dependenciesStore.Clear ();
			var q = obj.Types.OrderBy (x => x.Name);
			foreach (var t in q) {
				dependenciesStore.AddNode (new DependencyTreeNode (t));
			}
		}

		async void RunButton_Clicked (object sender, EventArgs e)
		{
			ClearAlert ();
			await Env.VisualizeAsync ();
		}

		async void RefreshButton_Clicked (object sender, EventArgs e)
		{
			ClearAlert ();
			await Env.VisualizeMonitoredTypeAsync (forceEval: true, showError: true);
		}

		async void StopButton_Clicked (object sender, EventArgs e)
		{
			ClearAlert ();
			await Env.StopVisualizingAsync ();
			dependenciesStore.Clear ();
		}

		async void ClearButton_Clicked (object sender, EventArgs e)
		{
			ClearAlert ();
			TypeCode.ClearEdits ();
			await Env.VisualizeMonitoredTypeAsync (forceEval: false, showError: false);
		}

		void HostEntry_Changed (object sender, EventArgs e)
		{
			Env.IP = hostEntry.Text;
		}
		//void PortEntry_Changed (object sender, EventArgs e)
		//{
		//	var port = Http.DefaultPort;
		//	if (!int.TryParse (portEntry.Text, out port))
		//	{
		//		port = Http.DefaultPort;
		//	}
		//	Env.Port = port;
		//}

		void ClearAlert ()
		{
			alertLabel.Text = "";
		}

		void Env_Alerted (string obj)
		{
			alertLabel.Text = obj;
		}
	}

	[Gtk.TreeNode (ListOnly=true)]
	public class DependencyTreeNode : TreeNode
	{
		public DependencyTreeNode (TypeCode type)
		{
			Name = type.Name;
			var timeStr = type.CodeChangedTime.ToLocalTime ().ToString ("T");
			Status = type.CodeChanged ? ("Edit " + timeStr) : "";
		}

		[Gtk.TreeNodeValue (Column=0)]
		public string Name;

		[Gtk.TreeNodeValue (Column=1)]
		public string Status;
	}
}

