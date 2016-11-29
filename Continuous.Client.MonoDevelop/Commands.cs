using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

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

