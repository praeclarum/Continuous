using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Gtk;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory;

namespace LiveCode.Client.XamarinStudio
{
	public enum Commands
	{
		VisualizeSelection,
		VisualizeClass,
	}

	public class LiveCodeCommandHandler : CommandHandler
	{
		HttpClient conn = null;
		protected void Connect ()
		{
			conn = new HttpClient (new Uri ("http://127.0.0.1:" + Http.DefaultPort));
		}

		protected void Alert (string format, params object[] args)
		{
			Console.WriteLine (format, args);
			var parentWindow = IdeApp.Workbench.RootWindow;
			var dialog = new MessageDialog(parentWindow, DialogFlags.DestroyWithParent,
				MessageType.Info, ButtonsType.Ok,
				format, args);
			dialog.Run ();
			dialog.Destroy ();
		}

		protected async Task<bool> EvalAsync (string code, bool showError)
		{
			var r = await conn.VisualizeAsync (code);
			var err = r.HasErrors;
			if (err) {
				var message = string.Join ("\n", r.Messages.Select (m => m.MessageType + ": " + m.Text));
				if (showError) {
					Alert ("{0}", message);
				}
			}
			return !err;
		}
	}

	public class VisualizeSelectionHandler : LiveCodeCommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();

			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				Connect ();
				var code = doc.Editor.SelectedText;

				try {
					await EvalAsync (code, showError: true);
				} catch (Exception ex) {
					Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
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

	public class VisualizeClassHandler : LiveCodeCommandHandler
	{
		string monitorTypeName = "";

		protected override async void Run ()
		{
			base.Run ();

			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc == null)
				return;

			var typedecl = await FindTypeAtCursor ();

			if (typedecl == null) {
				Alert ("Could not find a type at the cursor.");
				return;
			}
			
			StartMonitoring ();

			var typeName = typedecl.Name;
			var ns = typedecl.Parent as NamespaceDeclaration;

			var nsName = ns == null ? "" : ns.FullName;

			Console.WriteLine ("MONITOR {0} --- {1}", nsName, typeName);

			monitorTypeName = typeName;

			await VisualizeTypeAsync (showError: true);
		}

		async Task VisualizeTypeAsync (bool showError)
		{
			if (string.IsNullOrWhiteSpace (monitorTypeName))
				return;
			
			var doc = IdeApp.Workbench.ActiveDocument;
			var resolver = await doc.GetSharedResolver ();
			var selTypeDecl =
				resolver.RootNode.Descendants.
				OfType<TypeDeclaration> ().
				FirstOrDefault (x => x.Name == monitorTypeName);

			if (selTypeDecl == null)
				return;

			//
			// Rename it to make registered Objective-C types happy.
			// Thanks NRefactory for making this so easy.
			//
			var newName = monitorTypeName + DateTime.UtcNow.Ticks;
			var newDecl = (TypeDeclaration)selTypeDecl.Clone ();
			newDecl.Name = newName;

			//
			// Send the code to the device
			//
			try {
				Connect ();

				//
				// Send all the usings
				//
				var usings =
					resolver.RootNode.Descendants.
					OfType<UsingDeclaration> ().
					ToList ();
				foreach (var u in usings) {
					var ucode = u.ToString ();
					Console.WriteLine (ucode);
					if (!await EvalAsync (u.ToString (), showError)) return;
				}

				//
				// Declare the type
				//
				var declCode = newDecl.ToString ();
				Console.WriteLine (declCode);
				if (!await EvalAsync (declCode, showError)) return;

				//
				// New it up
				//
				var newCode = "new " + newName + "()";
				Console.WriteLine (newCode);
				if (!await EvalAsync (newCode, showError)) return;
			} catch (Exception ex) {
				Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
			}
		}

		async Task<TypeDeclaration> FindTypeAtCursor ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var resolver = await doc.GetSharedResolver ();
			var editLoc = doc.Editor.Caret.Location;
			var editTLoc = new TextLocation (editLoc.Line, editLoc.Column);
			var selTypeDecl =
				resolver.RootNode.Descendants.
				OfType<TypeDeclaration> ().
				FirstOrDefault (x => x.StartLocation <= editTLoc && editTLoc <= x.EndLocation);
			return selTypeDecl;
		}

		bool monitoring = false;
		void StartMonitoring ()
		{
			if (monitoring) return;

			IdeApp.Workbench.ActiveDocumentChanged += BindActiveDoc;
			BindActiveDoc (this, EventArgs.Empty);

			monitoring = true;
		}

		MonoDevelop.Ide.Gui.Document boundDoc = null;

		void BindActiveDoc (object sender, EventArgs e)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (boundDoc == doc) {
				return;
			}
			if (boundDoc != null) {				
				boundDoc.DocumentParsed -= ActiveDoc_DocumentParsed;
			}
			boundDoc = doc;
			if (boundDoc != null) {
				boundDoc.DocumentParsed += ActiveDoc_DocumentParsed;
			}
		}

		async void ActiveDoc_DocumentParsed (object sender, EventArgs e)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			Console.WriteLine ("DOC PARSED {0}", doc.Name);
			await VisualizeTypeAsync (showError: false);
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var doc = IdeApp.Workbench.ActiveDocument;

			info.Enabled = doc != null;
		}
	}
}

