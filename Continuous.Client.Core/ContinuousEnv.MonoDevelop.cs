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

		protected override async Task SetWatchTextAsync (WatchVariable w, List<string> vals)
		{
            var doc = IdeApp.Workbench.GetDocument (w.FilePath);
            if (doc == null)
                return;
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

		class CSharpTypeDecl : TypeDecl
		{
			public TypeDeclaration Declaration;
			public CSharpAstResolver Resolver;
			public override string Name {
				get {
					return Declaration.Name;
				}
			}
			public override TextLoc StartLocation {
				get {
					var l = Declaration.StartLocation;
					return new TextLoc {
						Line = l.Line,
						Column = l.Column,
					};
				}
			}
			public override TextLoc EndLocation {
				get {
					var l = Declaration.EndLocation;
					return new TextLoc {
						Line = l.Line,
						Column = l.Column,
					};
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
			public override TextLoc StartLocation {
				get {
					return new TextLoc (TextLoc.MinLine, TextLoc.MinColumn);
				}
			}
			public override TextLoc EndLocation {
				get {
					return new TextLoc (1000000, TextLoc.MinColumn);
				}
			}
			public override void SetTypeCode ()
			{
				Console.WriteLine ("SET XAML TYPE CODE");
			}
		}

		protected override async Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ()
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

		protected override async Task<TextLoc?> GetCursorLocationAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null) {
				return null;
			}

			var editLoc = doc.Editor.Caret.Location;
			return new TextLoc (editLoc.Line, editLoc.Column);
		}

		protected override void MonitorEditorChanges ()
		{
			IdeApp.Workbench.ActiveDocumentChanged += BindActiveDoc;
			BindActiveDoc (this, EventArgs.Empty);
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

		protected override async Task<string> GetSelectedTextAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				return doc.Editor.SelectedText;
			}

			return "";
		}
    }
}
#endif
