using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using EnvDTE80;
using System.Threading;

#if VISUALSTUDIO
using EnvDTE;
using System.Windows;

namespace Continuous.Client
{
    public partial class ContinuousEnv
    {
        static partial void SetSharedPlatformEnvImpl ()
        {
            Shared = new VisualStudioContinuousEnv ();
        }
    }

    public class VisualStudioContinuousEnv : ContinuousEnv
    {        
        protected override void AlertImpl(string format, params object[] args)
        {
            MessageBox.Show (string.Format (System.Globalization.CultureInfo.CurrentUICulture, format, args));
        }

        protected override async Task<TextLoc?> GetCursorLocationAsync ()
        {
            var dte = VisualStudio.ContinuousPackage.TheDTE;
            if (dte == null)
                return null;
            var doc = dte.ActiveDocument;
            if (doc == null)
                return null;
            var sel = doc.Selection as TextSelection;
            if (sel == null)
                return null;

            return new TextLoc (sel.CurrentLine, sel.CurrentColumn);
        }

        protected override async Task<string> GetSelectedTextAsync ()
        {
            var dte = VisualStudio.ContinuousPackage.TheDTE;
            if (dte == null)
                return null;
            var doc = dte.ActiveDocument;
            if (doc == null)
                return null;
            var sel = doc.Selection as TextSelection;
            if (sel == null)
                return null;

            return sel.Text;
        }

        void GetTopLevelTypeDecls (CodeElement elm, string nsName, Document doc, List<string> usings, List<TypeDecl> decls)
        {
            var k = elm.Kind;
            switch (k) {
                case vsCMElement.vsCMElementImportStmt: {
                        var cs = ((CodeImport)elm).Namespace;
                        usings.Add ("using " + cs + ";");
                    }
                    break;
                case vsCMElement.vsCMElementNamespace:
                    foreach (CodeElement e in elm.Children) {
                        GetTopLevelTypeDecls (e, elm.Name, doc, usings, decls);
                    }
                    break;
                case vsCMElement.vsCMElementClass:
                    decls.Add (new CodeTypeDecl (elm, nsName, doc, usings.ToArray ()));
                    break;
            }
        }

        protected override async Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ()
        {
            var dte = VisualStudio.ContinuousPackage.TheDTE;
            if (dte == null)
                throw new InvalidOperationException ("OMG Continuous Package has not inited yet");

            var doc = dte.ActiveDocument;
            var model = doc.ProjectItem.FileCodeModel;

            var decls = new List<TypeDecl> ();
            var usings = new List<string> ();

            foreach (CodeElement e in model.CodeElements) {
                GetTopLevelTypeDecls (e, "", doc, usings, decls);
            }

            return decls.ToArray ();
        }

        TextEditorEvents textEditorEvents = null;

        protected override void MonitorEditorChanges ()
        {
            var dte = VisualStudio.ContinuousPackage.TheDTE;
            if (dte == null)
                throw new InvalidOperationException ("OMG Continuous Package has not inited yet");

            textEditorEvents = dte.Events.TextEditorEvents; // Stupid COM binding requires explicit GC roots
            textEditorEvents.LineChanged += TextEditorEvents_LineChanged;
        }

		DateTime lastChangedTime = DateTime.UtcNow;
		Timer changedThrottleTimer = null;

        private async void TextEditorEvents_LineChanged (TextPoint StartPoint, TextPoint EndPoint, int Hint)
        {
			var now = DateTime.UtcNow;
			lastChangedTime = now;
			if (changedThrottleTimer == null) {
				changedThrottleTimer = new Timer (LineChangedThrottleTick, null, 0, 200);
			}
        }

		async void LineChangedThrottleTick (object state)
		{
			var now = DateTime.UtcNow;
			if ((now - lastChangedTime) > TimeSpan.FromMilliseconds (500)) {
				if (changedThrottleTimer != null) {
					changedThrottleTimer.Dispose ();
					changedThrottleTimer = null;
				}
				await SetTypesAndVisualizeMonitoredTypeAsync (forceEval: false, showError: false);
			}
		}

        protected override async Task SetWatchTextAsync (WatchVariable w, List<string> vals)
        {
            // throw new NotImplementedException ();
        }

        class CodeTypeDecl : TypeDecl
        {
            public readonly CodeElement Element;
            readonly TextLoc startLoc, endLoc;
            readonly string name, nsName;
            readonly string rawCode;
            readonly string[] usings;
            public CodeTypeDecl (CodeElement elm, string nsName, Document doc, string[] usings)
            {
                Element = elm;
                name = Element.Name;
                this.nsName = nsName;
                var point = elm.StartPoint.CreateEditPoint ();
                rawCode = point.GetText (elm.EndPoint);
                startLoc = TextLocFromPoint (elm.StartPoint);
                endLoc = TextLocFromPoint (elm.EndPoint);
                this.usings = usings;
            }
            TextLoc TextLocFromPoint (TextPoint l)
            {
                return new TextLoc {
                    Line = l.Line,
                    Column = l.DisplayColumn,
                };
            }
            public override string Name {
                get {
                    return name;
                }
            }
            public override TextLoc StartLocation {
                get {
                    return startLoc;
                }
            }
            public override TextLoc EndLocation {
                get {
                    return endLoc;
                }
            }
            public override void SetTypeCode ()
            {
                var name = Name;

                var watches = new List<WatchVariable> ();

                var deps = new List<string> ();

                var commentlessCode = rawCode;
                var instrumentedCode = commentlessCode;

                TypeCode.Set (name, usings, rawCode, instrumentedCode, deps, nsName, watches);
            }
        }
    }
}
#endif
