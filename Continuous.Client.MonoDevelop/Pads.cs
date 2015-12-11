using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Gtk;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory;
using System.Collections.Generic;
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
		readonly Button runButton = new Button { Label = "Visualize" };
		readonly Button stopButton = new Button { Label = "Stop" };
		readonly Button clearButton = new Button { Label = "Clear Edits" };
		readonly NodeStore typesStore = new NodeStore (typeof(TypeTreeNode));
		readonly NodeView typesView;
		readonly Label errorLabel = new Label ("Errors") {
			Justify = Justification.Left,
		};

		public MainPadControl ()
		{
			Env.LinkedMonitoredCode += Env_LinkedMonitoredCode;

			runButton.Clicked += RunButton_Clicked;
			stopButton.Clicked += StopButton_Clicked;
			clearButton.Clicked += ClearButton_Clicked;

			typesView = new NodeView (typesStore);
			typesView.AppendColumn ("Type", new CellRendererText (), "text", 0);
			typesView.AppendColumn ("Status", new CellRendererText (), "text", 1);

			toolbar.PackStart (runButton, false, false, 6);
			toolbar.PackStart (stopButton, false, false, 6);
			toolbar.PackEnd (clearButton, false, false, 6);
			PackStart (toolbar, false, false, 0);
//			PackStart (errorLabel, false, false, 8);
			PackEnd (typesView, true, true, 0);
		}

		void Env_LinkedMonitoredCode (LinkedCode obj)
		{
			typesStore.Clear ();
			var q = obj.Types.OrderBy (x => x.Name);
			foreach (var t in q) {
				typesStore.AddNode (new TypeTreeNode (t));
			}
		}

		async void ClearButton_Clicked (object sender, EventArgs e)
		{
			TypeCode.ClearEdits ();
			await Env.RevisualizeAsync (forceEval: false, showError: false);
		}

		async void RunButton_Clicked (object sender, EventArgs e)
		{
			await Env.VisualizeAsync ();
		}

		async void StopButton_Clicked (object sender, EventArgs e)
		{
			await Env.StopVisualizingAsync ();
			typesStore.Clear ();
		}
	}

	[Gtk.TreeNode (ListOnly=true)]
	public class TypeTreeNode : TreeNode
	{
		public TypeTreeNode (TypeCode type)
		{
			Name = type.Name;
			var timeStr = type.CodeChangedTime.ToLocalTime ().ToString ("T");
			Status = type.CodeChanged ? ("Edited " + timeStr) : "";
		}

		[Gtk.TreeNodeValue (Column=0)]
		public string Name;

		[Gtk.TreeNodeValue (Column=1)]
		public string Status;
	}
}

