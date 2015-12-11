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

namespace Continuous.Client.XamarinStudio
{
	public enum Commands
	{
		VisualizeSelection,
		VisualizeClass,
		StopVisualizingClass,
	}

	public class ContinuousCommandHandler : CommandHandler
	{
		protected ContinuousEnv Env { get { return ContinuousEnv.Shared; } }
	}

	public class VisualizeSelectionHandler : ContinuousCommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();

			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				var code = doc.Editor.SelectedText;

				try {
					await Env.EvalAsync (code, showError: true);
				} catch (Exception ex) {
					Env.Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
				}
			}
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var doc = IdeApp.Workbench.ActiveDocument;

			info.Enabled = doc != null && doc.Editor != null && !string.IsNullOrWhiteSpace (doc.Editor.SelectedText);
		}
	}

	public class VisualizeClassHandler : ContinuousCommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();
			await Env.VisualizeAsync ();
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var doc = IdeApp.Workbench.ActiveDocument;

			info.Enabled = doc != null;
		}
	}

	public class StopVisualizingClassHandler : ContinuousCommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();
			await Env.StopVisualizingAsync ();
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			string t = Env.MonitorTypeName;

			if (string.IsNullOrWhiteSpace (t)) {
				info.Text = "Stop Visualizing Class";
				info.Enabled = false;
			}
			else {
				info.Text = "Stop Visualizing " + t;
				info.Enabled = true;
			}
		}
	}
}

