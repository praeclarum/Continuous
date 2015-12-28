using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

#if MONODEVELOP
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Refactoring;
using Gtk;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;


namespace Continuous.Client
{
    public partial class ContinuousEnv
    {
		static partial void SetSharedPlatformEnvImpl ()
		{
			Shared = new MonoDevelopContinuousEnv ();
		}
	}

	public class MonoDevelopContinuousEnv : ContinuousEnv
	{
		protected override void AlertImpl (string format, params object[] args)
        {
			var parentWindow = IdeApp.Workbench.RootWindow;
			var dialog = new MessageDialog(parentWindow, DialogFlags.DestroyWithParent,
				MessageType.Info, ButtonsType.Ok,
				false,
				format, args);
			dialog.Run ();
			dialog.Destroy ();
        }

		async Task MonitorWatchChanges ()
		{
			var version = 0L;
			var conn = CreateConnection ();
			for (;;) {
				try {
//					Console.WriteLine ("MON WATCH " + DateTime.Now);
					var res = await conn.WatchChangesAsync (version);
					if (res != null) {
						version = res.Version;
						await UpdateEditorWatchesAsync (res);
					}
					else {
						await Task.Delay (1000);
					}
				} catch (Exception ex) {
					Console.WriteLine (ex);
					await Task.Delay (3000);
				}
			}
		}

		public override async Task VisualizeAsync ()
		{
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

			MonitorTypeName = typeName;
			//			monitorNamespace = nsName;

			await SetTypesAndVisualizeMonitoredTypeAsync (forceEval: true, showError: true);
		}

		LinkedCode lastLinkedCode = null;

		async Task SetTypesAndVisualizeMonitoredTypeAsync (bool forceEval, bool showError)
		{
			//
			// Gobble up all we can about the types in the active document
			//
			var typeDecls = await GetTopLevelTypeDeclsAsync ();
			foreach (var td in typeDecls) {
				td.SetTypeCode ();
			}

			await VisualizeMonitoredTypeAsync (forceEval, showError);
		}

		public override async Task VisualizeMonitoredTypeAsync (bool forceEval, bool showError)
		{
			//
			// Refresh the monitored type
			//
			if (string.IsNullOrWhiteSpace (MonitorTypeName))
				return;

			var monitorTC = TypeCode.Get (MonitorTypeName);

			var code = await Task.Run (() => monitorTC.GetLinkedCode ());

			OnLinkedMonitoredCode (code);

			if (!forceEval && lastLinkedCode != null && lastLinkedCode.CacheKey == code.CacheKey) {
				return;
			}

			//
			// Send the code to the device
			//
			try {
				//
				// Declare it
				//
				Log (code.Declarations);
				if (!await EvalAsync (code.Declarations, showError)) return;

				//
				// Show it
				//
				Log (code.ValueExpression);
				var resp = await EvalForResponseAsync (code.ValueExpression, showError);
				if (resp.HasErrors)
					return;

				//
				// If we made it this far, remember so we don't re-send the same
				// thing immediately
				//
				lastLinkedCode = code;

				//
				// Update the editor
				//
				await UpdateEditorAsync (code, resp);

			} catch (Exception ex) {
				if (showError) {
					Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
				}
			}
		}

		async Task UpdateEditorAsync (LinkedCode code, EvalResponse resp)
		{
			await UpdateEditorWatchesAsync (code.Types.SelectMany (x => x.Watches), resp.WatchValues);
		}

		List<WatchVariable> lastWatches = new List<WatchVariable> ();

		async Task UpdateEditorWatchesAsync (WatchValuesResponse watchValues)
		{
			await UpdateEditorWatchesAsync (lastWatches, watchValues.WatchValues);
		}

		async Task UpdateEditorWatchesAsync (IEnumerable<WatchVariable> watches, Dictionary<string, List<string>> watchValues)
		{
			var ws = watches.ToList ();
			foreach (var w in ws) {
				var wd = IdeApp.Workbench.GetDocument (w.FilePath);
				if (wd == null)
					continue;
				List<string> vals;
				if (!watchValues.TryGetValue (w.Id, out vals)) {
					vals = new List<string> ();
				}
//				Console.WriteLine ("VAL {0} {1} = {2}", w.Id, w.Expression, vals);
				SetWatchText (w, vals, wd);
			}
			lastWatches = ws;
		}

		string GetValsText (List<string> vals)
		{
			var maxLength = 72;
			var newText = string.Join (", ", vals);
			newText = newText.Replace ("\r\n", " ").Replace ("\n", " ").Replace ("\t", " ");
			if (newText.Length > maxLength) {
				newText = "..." + newText.Substring (newText.Length - maxLength);
			}
			return newText;
		}

		void SetWatchText (WatchVariable w, List<string> vals, Document doc)
		{
			var ed = doc.Editor;
			if (ed == null || !ed.CanEdit (w.FileLine))
				return;
			var line = ed.GetLine (w.FileLine);
			if (line == null)
				return;
			var newText = "//" + w.ExplicitExpression + "= " + GetValsText (vals);
			var lineText = ed.GetLineText (w.FileLine);
			var commentIndex = lineText.IndexOf ("//");
			if (commentIndex < 0)
				return;
			var commentCol = commentIndex + 1;
			if (commentCol != w.FileColumn)
				return;

			var existingText = lineText.Substring (commentIndex);

			if (existingText != newText) {
				var offset = line.Offset + commentIndex;
				var remLen = line.Length - commentIndex;
				ed.Remove (offset, remLen);
				ed.Insert (offset, newText);
			}
		}

		abstract class TypeDecl
		{
			public DocumentRef Document { get; set; }
			public abstract string Name { get; }
			public abstract TextLocation StartLocation { get; }
			public abstract TextLocation EndLocation { get; }
			public abstract void SetTypeCode ();
		}

		class CSharpTypeDecl : TypeDecl
		{
			public TypeDeclaration Declaration;
			public CSharpAstResolver Resolver;
			public override string Name {
				get {
					return Declaration.Name;
				}
			}
			public override TextLocation StartLocation {
				get {
					return Declaration.StartLocation;
				}
			}
			public override TextLocation EndLocation {
				get {
					return Declaration.EndLocation;
				}
			}
			public override void SetTypeCode ()
			{
				TypeCode.Set (Document, Declaration, Resolver);
			}
		}

		class XamlTypeDecl : TypeDecl
		{
			public string XamlText;
			public override string Name {
				get {
					Console.WriteLine ("XAML TYPE GET NAME");
					return "??";
				}
			}
			public override TextLocation StartLocation {
				get {
					return new TextLocation (TextLocation.MinLine, TextLocation.MinColumn);
				}
			}
			public override TextLocation EndLocation {
				get {
					return new TextLocation (1000000, TextLocation.MinColumn);
				}
			}
			public override void SetTypeCode ()
			{
				Console.WriteLine ("SET XAML TYPE CODE");
			}
		}

		async Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			Log ("Doc = {0}", doc);
			if (doc == null) {
				return new TypeDecl[0];
			}

			var ext = doc.FileName.Extension;

			if (ext == ".cs") {
				try {					
					var resolver = await doc.GetSharedResolver ();
					var typeDecls =
						resolver.RootNode.Descendants.
						OfType<TypeDeclaration> ().
						Where (x => !(x.Parent is TypeDeclaration)).
						Select (x => new CSharpTypeDecl {
							Document = new DocumentRef (doc.FileName.FullPath),
							Declaration = x,
							Resolver = resolver,
						});
					return typeDecls.ToArray ();
				} catch (Exception ex) {
					Log (ex);
					return new TypeDecl[0];
				}
			}

			if (ext == ".xaml") {
				var xaml = doc.Editor.Text;
				return new TypeDecl[] {
					new XamlTypeDecl {
						Document = new DocumentRef (doc.FileName.FullPath),
						XamlText = xaml,
					},
				};
			}

			return new TypeDecl[0];
		}

		async Task<TypeDecl> FindTypeAtCursor ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null) {
				return null;
			}

			var editLoc = doc.Editor.Caret.Location;
			var editTLoc = new TextLocation (editLoc.Line, editLoc.Column);

			var selTypeDecl =
				(await GetTopLevelTypeDeclsAsync ()).
				FirstOrDefault (x => x.StartLocation <= editTLoc && editTLoc <= x.EndLocation);
			return selTypeDecl;
		}

		bool monitoring = false;
		void StartMonitoring ()
		{
			if (monitoring) return;

			IdeApp.Workbench.ActiveDocumentChanged += BindActiveDoc;
			BindActiveDoc (this, EventArgs.Empty);

			MonitorWatchChanges ();

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
			Log ("DOC PARSED {0}", doc.Name);
			await SetTypesAndVisualizeMonitoredTypeAsync (forceEval: false, showError: false);
		}

		public override async Task VisualizeSelectionAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				var code = doc.Editor.SelectedText;

				try {
					await EvalAsync (code, showError: true);
				} catch (Exception ex) {
					Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
				}
			}
		}

    }
}
#endif
