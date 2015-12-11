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
		MainPadControl control = new MainPadControl ();

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
		readonly HBox toolbar = new HBox ();
		readonly ToggleButton runButton = new ToggleButton { Label = "Do it" };
		readonly NodeStore typesStore = new NodeStore (typeof(TypeTreeNode));
		readonly NodeView typesView;
		readonly Label errorLabel = new Label ("Errors") {
			Justify = Justification.Left,
		};

		public MainPadControl ()
		{
			typesStore.AddNode (new TypeTreeNode ("Hello"));
			typesStore.AddNode (new TypeTreeNode ("World"));

			typesView = new NodeView (typesStore);
			typesView.AppendColumn ("Type", new CellRendererText (), "text", 0);
			typesView.AppendColumn ("Status", new CellRendererText (), "text", 1);

			runButton.Active = true;
			toolbar.PackStart (runButton, false, false, 8);
			PackStart (toolbar, false, false, 0);
			PackStart (errorLabel, false, false, 8);
			PackEnd (typesView, true, true, 0);
		}
	}

	[Gtk.TreeNode (ListOnly=true)]
	public class TypeTreeNode : TreeNode
	{
		public TypeTreeNode (string name)
		{
			Name = name;
			Status = "";
		}

		[Gtk.TreeNodeValue (Column=0)]
		public string Name;

		[Gtk.TreeNodeValue (Column=1)]
		public string Status;
	}
}

