using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

using MonoDevelop.Ide;
using Gtk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Text.Editor;
using Continuous.Client.MD.Extensions;
using System.Threading;

#pragma warning disable 1998

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
		protected override async Task SetWatchTextAsync (WatchVariable w, List<string> vals)
		{
            Console.WriteLine("Ignoring SetWatchTextAsync");

            return;

            /*
            var doc = IdeApp.Workbench.GetDocument(w.FilePath);
            if (doc == null)
                return;

			var ed = doc.Editor;

            if (ed == null || ed.IsReadOnly)
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
				ed.RemoveText (offset, remLen);
				ed.InsertText (offset, newText);
			}
            */

		}

		class CSharpTypeDecl : TypeDecl
		{
			public ClassDeclarationSyntax Declaration { get; set; }
			public SyntaxNode Root { get; set; }
			public SemanticModel Model { get; set; }

			public override string Name {
				get {
					return Declaration.Identifier.Text;
				}
			}

			public override TextLoc StartLocation {
				get {
					var l = Declaration.GetLocation ().GetLineSpan ().StartLinePosition;
					return new TextLoc {
						Line = l.Line,
						Column = l.Character
					};
				}
			}

			public override TextLoc EndLocation {
				get {
					var l = Declaration.GetLocation ().GetLineSpan ().EndLinePosition;
					return new TextLoc {
						Line = l.Line,
						Column = l.Character
					};
				}
			}

			public override void SetTypeCode ()
			{
				var name = Name;

				var commentlessCode = String.Join (Environment.NewLine, Declaration.GetText ().Lines.Select (l => l.ToString ()));

				var usings =
					Root.DescendantNodes ()
						.OfType<UsingDirectiveSyntax> ()
						.Select (u => u.GetText ().ToString ())
						.ToList ();

				var deps =
					Root.DescendantNodes()
					    .OfType<IdentifierNameSyntax>()
						.Select(n => Model.GetSymbolInfo(n))
						.Where(s => s.Symbol != null && s.Symbol.Kind == SymbolKind.NamedType)
						.Select(n => n.Symbol.Name)
						.Distinct()
					    .ToList();

				var ns =
					Declaration.Ancestors()
					           .OfType<NamespaceDeclarationSyntax>()
							   .Select(n => n.Name.GetText().ToString().Trim())
							   .FirstOrDefault() ?? "";

				// create an 'instrumented' instance of the document with watch calls
				var rewriter = new WatchExpressionRewriter(Document.FullPath);
				var instrumented = rewriter.Visit(Declaration);
				var instrumentedCode = instrumented.ToString();

				// the rewriter collects the WatchVariable definitions as it walks the tree
				var watches = rewriter.WatchVariables;

				TypeCode.Set (name, usings, commentlessCode, instrumentedCode, deps, ns, watches);
			}
		}

		class XamlTypeDecl : TypeDecl
		{
            public string XamlText = String.Empty;
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

                    var analysisDocument = doc.GetAnalysisDocument();

					var root = await analysisDocument.GetSyntaxRootAsync ();
					var model = await analysisDocument.GetSemanticModelAsync ();
					var typeDecls =
						root.DescendantNodes ((arg) => true)
							.OfType<ClassDeclarationSyntax> ()
							.Select (t => new CSharpTypeDecl {
								Document = new DocumentRef (doc.FileName.FullPath),
								Declaration = t,
								Root = root,
								Model = model,
							});
					return typeDecls.ToArray ();

				} catch (Exception ex) {
					Log (ex);
					return new TypeDecl[0];
				}
			}
            
			return new TypeDecl[0];
		}

		protected override async Task<TextLoc?> GetCursorLocationAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null) {
				return null;
			}

            var editLoc = doc.GetContent<ITextView>().Caret.Position.BufferPosition.GetLineAndColumn1Based();
			return new TextLoc (editLoc.line, editLoc.column);
		}

		protected override void MonitorEditorChanges ()
		{
			IdeApp.Workbench.ActiveDocumentChanged += BindActiveDoc;
			BindActiveDoc (this, EventArgs.Empty);
		}
        
		MonoDevelop.Ide.Gui.Document boundDoc = null;
        ITextBuffer buffer = null;

		void BindActiveDoc (object sender, EventArgs e)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
            
			if (boundDoc == doc) {
				return;
			}
            
			if (buffer != null) {
                buffer.Changed -= ScheduleUpdate; 
            }

			boundDoc = doc;
            buffer = doc.GetContent<ITextBuffer>();

            if (buffer != null) {
                buffer.Changed += ScheduleUpdate;
			}
        }

        CancellationTokenSource _canceller = new CancellationTokenSource();
        public void ScheduleUpdate(object sender, EventArgs e)
        {
            _canceller.Cancel();
            _canceller = new CancellationTokenSource();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Delay(350, _canceller.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCanceled)
                        return;

                    ActiveDoc_DocumentParsed(null, null);
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        async void ActiveDoc_DocumentParsed (object sender, EventArgs e)
		{
            var doc = IdeApp.Workbench.ActiveDocument;
			Log ("DOC PARSED {0}", doc.Name);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await SetTypesAndVisualizeMonitoredTypeAsync(forceEval: false, showError: false));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

		protected override async Task<string> GetSelectedTextAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
            if (doc == null)
                return "";

            var selections = doc.GetContent<ITextView>().Selection.SelectedSpans;
            if (!selections.Any())
                return "";

            var selection = selections.First();

            return selection.GetText();
		}
    }

	public class WatchExpressionRewriter : CSharpSyntaxRewriter
	{
		private string path;

		public WatchExpressionRewriter(string p)
		{
			path = p;
		}

		public List<WatchVariable> WatchVariables = new List<WatchVariable>();

		public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
		{
			// don't handle nodes that aren't assignment or don't have the //= comment
			if (!(node.Expression is AssignmentExpressionSyntax && HasWatchComment(node)))
				return node;

			var expr = (node.Expression as AssignmentExpressionSyntax).Left;
			return AddWatchNode(node, expr);
		}

		public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
		{
			// don't handle nodes that don't have the //= comment
			if (!HasWatchComment(node))
				return node;

			var vn = node.Declaration.Variables.First().Identifier.Text;
			var expr = SyntaxFactory.IdentifierName(vn);

			return AddWatchNode(node, expr);
		}

		bool HasWatchComment(SyntaxNode node)
		{
			// expect that only valid node types are passed here
			return
				node
					.GetTrailingTrivia()
					.Any(t => t.Kind() == SyntaxKind.SingleLineCommentTrivia && t.ToString().StartsWith("//=", StringComparison.InvariantCultureIgnoreCase));
		}

		SyntaxNode AddWatchNode(StatementSyntax node, ExpressionSyntax expr)
		{
			var id = Guid.NewGuid().ToString();
			var c = node.GetTrailingTrivia().First(t => t.Kind() == SyntaxKind.SingleLineCommentTrivia && t.ToString().StartsWith("//=", StringComparison.InvariantCultureIgnoreCase));
			var p = c.GetLocation().GetLineSpan().StartLinePosition;

			var wv = new WatchVariable {
				Id = id,
				Expression = expr.ToString(),
				ExplicitExpression = "",
				FilePath = path,
				FileLine = p.Line + 1,  // 0-based index
				FileColumn = p.Character + 1, // 0-based index
			};
			WatchVariables.Add(wv);

			var wi = GetWatchInstrument(id, expr);

			// creating a block and removing the open/close braces is a bit of a hack but
			// lets us replace one node with two... 
			return
				SyntaxFactory
					.Block(node, wi)
					.WithOpenBraceToken(SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken))
					.WithCloseBraceToken(SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken))
					.WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"));
		}

		StatementSyntax GetWatchInstrument(string id, ExpressionSyntax expr)
		{
			return SyntaxFactory.ParseStatement($"try {{ Continuous.Server.WatchStore.Record(\"{id}\", {expr}); }} catch {{ }}");
		}
	}	
}