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
				Set (Document, Declaration, Resolver);
			}

	        static Statement GetWatchInstrument (string id, Expression expr)
			{
				var r = new MemberReferenceExpression (
					new MemberReferenceExpression (
						new MemberReferenceExpression (
							new IdentifierExpression ("Continuous"), "Server"), "WatchStore"), "Record");
				var i = new ExpressionStatement (new InvocationExpression (r, new PrimitiveExpression (id), expr));
				var t = new TryCatchStatement ();
				t.TryBlock = new BlockStatement ();
				t.TryBlock.Statements.Add (i);
				var c = new CatchClause ();
				c.Body = new BlockStatement ();
				t.CatchClauses.Add (c);
				return t;
			}

			static string GetCommentlessCode (TypeDeclaration rtypedecl)
			{
				var t = (TypeDeclaration)rtypedecl.Clone ();
				var cs = t.Descendants.OfType<Comment> ().ToList ();
				foreach (var c in cs) {
					var m = WatchVariable.CommentContentRe.Match (c.Content);
					if (m.Success) {
						c.Content = m.Groups[1].Value + m.Groups[2].Value;
					} else {
						c.Remove ();
					}
				}
				return t.ToString ();
			}

			static TypeCode Set (DocumentRef doc, TypeDeclaration rtypedecl, CSharpAstResolver resolver)
			{
				var rawCode = GetCommentlessCode (rtypedecl);

				var typedecl = (TypeDeclaration)rtypedecl.Clone ();

				var ns = rtypedecl.Parent as NamespaceDeclaration;
				var nsName = ns == null ? "" : ns.FullName;

				var name = rtypedecl.Name;

				var usings =
					resolver.RootNode.Descendants.
					OfType<UsingDeclaration> ().
					Select (x => x.ToString ().Trim ()).
					ToList ();

				//
				// Find dependencies
				//
				var deps = new List<String> ();
				foreach (var d in rtypedecl.Descendants.OfType<SimpleType> ()) {
					deps.Add (d.Identifier);
				}

				//
				// Find watches and instrument
				//
				var watches = new List<WatchVariable> ();
				foreach (var d in typedecl.Descendants.OfType<VariableInitializer> ()) {
					var endLoc = d.EndLocation;
					var p = d.Parent;
					if (p == null || p.Parent == null)
						continue;
					var nc = p.GetNextSibling (x => x is Comment && x.StartLocation.Line == endLoc.Line);
					if (nc == null || !nc.ToString ().StartsWith ("//="))
						continue;
					var id = Guid.NewGuid ().ToString ();
					var instrument = GetWatchInstrument (id, new IdentifierExpression (d.Name));
					p.Parent.InsertChildBefore (nc, instrument, BlockStatement.StatementRole);
					watches.Add (new WatchVariable {
						Id = id,
						Expression = d.Name,
						ExplicitExpression = "",
						FilePath = doc.FullPath,
						FileLine = nc.StartLocation.Line,
						FileColumn = nc.StartLocation.Column,
					});
				}
				foreach (var d in typedecl.Descendants.OfType<AssignmentExpression> ()) {
					var endLoc = d.EndLocation;
					var p = d.Parent;
					if (p == null || p.Parent == null)
						continue;
					var nc = p.GetNextSibling (x => x is Comment && x.StartLocation.Line == endLoc.Line);
					if (nc == null || !nc.ToString ().StartsWith ("//="))
						continue;
					var id = Guid.NewGuid ().ToString ();
					var instrument = GetWatchInstrument (id, (Expression)d.Left.Clone ());
					p.Parent.InsertChildBefore (nc, instrument, BlockStatement.StatementRole);
					watches.Add (new WatchVariable {
						Id = id,
						Expression = d.Left.ToString (),
						ExplicitExpression = "",
						FilePath = doc.FullPath,
						FileLine = nc.StartLocation.Line,
						FileColumn = nc.StartLocation.Column,
					});
				}
				foreach (var d in typedecl.Descendants.OfType<Comment> ().Where (x => x.CommentType == CommentType.SingleLine)) {
					var m = WatchVariable.CommentContentRe.Match (d.Content);
					if (!m.Success || string.IsNullOrWhiteSpace (m.Groups [1].Value))
						continue;

					var p = d.Parent as BlockStatement;
					if (p == null)
						continue;
					
					var exprText = m.Groups [1].Value.Trim ();
					var parser = new CSharpParser();
					var syntaxTree = parser.Parse("class C { void F() { var __r = " + exprText + "; } }");

					if (syntaxTree.Errors.Count > 0)
						continue;
					var t = syntaxTree.Members.OfType<TypeDeclaration> ().First ();
					var expr = t.Descendants.OfType<VariableInitializer> ().First ().Initializer;
						
					var id = Guid.NewGuid ().ToString ();
					var instrument = GetWatchInstrument (id, expr.Clone ());
					p.InsertChildBefore (d, instrument, BlockStatement.StatementRole);
					watches.Add (new WatchVariable {
						Id = id,
						Expression = exprText,
						ExplicitExpression = m.Groups[1].Value,
						FilePath = doc.FullPath,
						FileLine = d.StartLocation.Line,
						FileColumn = d.StartLocation.Column,
					});
				}

				//
				// All done
				//
				var instrumentedCode = typedecl.ToString ();

				return TypeCode.Set (name, usings, rawCode, instrumentedCode, deps, nsName, watches);
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
