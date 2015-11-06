using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Gtk;
using System.Linq;

namespace LiveCode.Client.XamarinStudio
{
	public enum Commands
	{
		VisualizeSelection,
		VisualizeClass,
	}

	public class LiveCodeCommandHandler : CommandHandler
	{
		protected HttpClient Connect ()
		{
			return new HttpClient (new Uri ("http://127.0.0.1:" + Http.DefaultPort));
		}
	}

	public class VisualizeSelectionHandler : LiveCodeCommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();

			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				var conn = Connect ();
				var code = doc.Editor.SelectedText;
				await conn.VisualizeAsync (code);
			}
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var doc = IdeApp.Workbench.ActiveDocument;

			info.Enabled = doc != null && !string.IsNullOrWhiteSpace (doc.Editor.SelectedText);
		}
	}

	public class VisualizeClassHandler : LiveCodeCommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();

			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				var conn = Connect ();
				var code = doc.Editor.Text;
//				var resolver = await doc.GetSharedResolver ();
				await conn.VisualizeAsync (code);
			}
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var doc = IdeApp.Workbench.ActiveDocument;

			info.Enabled = doc != null;
		}
	}
}

