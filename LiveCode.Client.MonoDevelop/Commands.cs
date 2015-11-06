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
		ExecuteCode,
	}

	public class ExecuteCodeHandler : CommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();

//			var parentWindow = IdeApp.Workbench.RootWindow;
			var doc = IdeApp.Workbench.ActiveDocument;

			var conn = new HttpClient (new Uri ("http://127.0.01:" + Http.DefaultPort));

			if (doc != null) {
				var code = doc.Editor.Text.Substring (0, 140);
//				var resolver = await doc.GetSharedResolver ();
				var result = await conn.VisualizeAsync (code);
			}

			//			MessageDialog dialog = new MessageDialog(parentWindow, DialogFlags.DestroyWithParent,
			//				MessageType.Info, ButtonsType.Ok,
			//				"{0}", message);
			//			dialog.Run ();
			//			dialog.Destroy ();
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);
			info.Enabled = true;
		}
	}
}

