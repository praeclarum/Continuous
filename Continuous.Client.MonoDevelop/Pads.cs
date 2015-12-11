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
		readonly Button clearButton = new Button { Label = "Forget All" };
		readonly NodeStore typesStore = new NodeStore (typeof(TypeTreeNode));
		readonly NodeView typesView;
		readonly Label errorLabel = new Label ("Errors") {
			Justify = Justification.Left,
		};

		public MainPadControl ()
		{
			clearButton.Clicked += ClearButton_Clicked;

			typesView = new NodeView (typesStore);
			typesView.AppendColumn ("Type", new CellRendererText (), "text", 0);
			typesView.AppendColumn ("Status", new CellRendererText (), "text", 1);

			toolbar.PackStart (runButton, false, false, 8);
			toolbar.PackStart (stopButton, false, false, 8);
			toolbar.PackEnd (clearButton, false, false, 8);
			PackStart (toolbar, false, false, 0);
			PackStart (errorLabel, false, false, 8);
			PackEnd (typesView, true, true, 0);

			RefreshTypesStore ();
		}

		void ClearButton_Clicked (object sender, EventArgs e)
		{
			TypeCode.Clear ();
			RefreshTypesStore ();
		}

		void RefreshTypesStore ()
		{
			typesStore.Clear ();
			foreach (var t in TypeCode.All) {
				typesStore.AddNode (new TypeTreeNode (t));
			}
		}
	}

	[Gtk.TreeNode (ListOnly=true)]
	public class TypeTreeNode : TreeNode
	{
		public TypeTreeNode (TypeCode type)
		{
			Name = type.Name;
			Status = type.CodeChanged ? "Edited" : "";
		}

		[Gtk.TreeNodeValue (Column=0)]
		public string Name;

		[Gtk.TreeNodeValue (Column=1)]
		public string Status;
	}
}

