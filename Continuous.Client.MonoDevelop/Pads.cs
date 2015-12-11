using System;
using System.Linq;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace Continuous.Client.XamarinStudio
{
	public enum Pads
	{
		Main,
	}

	public class MainPad : IPadContent
	{
		readonly MainPadControl control = new MainPadControl ();

		#region IPadContent implementation
		public void Initialize (IPadWindow window)
		{
			control.ShowAll ();
		}
		public void RedrawContent ()
		{
		}
		public Widget Control {
			get {
				return control;
			}
		}
		#endregion
		#region IDisposable implementation
		public void Dispose ()
		{
			control.Dispose ();
		}
		#endregion
	}

	public class MainPadControl : VBox
	{
		protected ContinuousEnv Env { get { return ContinuousEnv.Shared; } }

		readonly HBox toolbar = new HBox ();
		readonly Button runButton = new Button { Label = "Set Type" };
		readonly Button refreshButton = new Button { Label = "Refresh" };
		readonly Button stopButton = new Button { Label = "Stop" };
		readonly Button clearButton = new Button { Label = "Clear Edits" };
		readonly NodeStore dependenciesStore = new NodeStore (typeof(DependencyTreeNode));
		readonly NodeView dependenciesView;
//		readonly Label errorLabel = new Label ("Errors") {
//			Justify = Justification.Left,
//		};

		public MainPadControl ()
		{
			Env.LinkedMonitoredCode += Env_LinkedMonitoredCode;

			runButton.Clicked += RunButton_Clicked;
			refreshButton.Clicked += RefreshButton_Clicked;
			stopButton.Clicked += StopButton_Clicked;
			clearButton.Clicked += ClearButton_Clicked;

			dependenciesView = new NodeView (dependenciesStore);
			dependenciesView.AppendColumn ("Dependency", new CellRendererText (), "text", 0);
			dependenciesView.AppendColumn ("Status", new CellRendererText (), "text", 1);

			toolbar.PackStart (runButton, false, false, 4);
			toolbar.PackStart (refreshButton, false, false, 4);
			toolbar.PackStart (stopButton, false, false, 4);
			toolbar.PackEnd (clearButton, false, false, 4);
			PackStart (toolbar, false, false, 0);
//			PackStart (errorLabel, false, false, 8);
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
			await Env.VisualizeAsync ();
		}

		async void RefreshButton_Clicked (object sender, EventArgs e)
		{
			await Env.VisualizeMonitoredTypeAsync (forceEval: true, showError: true);
		}

		async void StopButton_Clicked (object sender, EventArgs e)
		{
			await Env.StopVisualizingAsync ();
			dependenciesStore.Clear ();
		}

		async void ClearButton_Clicked (object sender, EventArgs e)
		{
			TypeCode.ClearEdits ();
			await Env.VisualizeMonitoredTypeAsync (forceEval: false, showError: false);
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

