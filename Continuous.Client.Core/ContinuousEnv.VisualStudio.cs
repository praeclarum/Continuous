using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

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

        void GetTopLevelTypeDecls (CodeElement elm, List<TypeDecl> decls)
        {
            var k = elm.Kind;
            switch (k) {
                case vsCMElement.vsCMElementNamespace: {
                        foreach (CodeElement e in elm.Children) {
                            GetTopLevelTypeDecls (e, decls);
                        }
                    }
                    break;
                case vsCMElement.vsCMElementClass:
                    decls.Add (new CodeTypeDecl (elm));
                    break;
            }
        }

        protected override async Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ()
        {
            var dte = VisualStudio.ContinuousPackage.TheDTE;
            if (dte == null)
                throw new InvalidOperationException ("OMG Continuous Package has not inited yet");

            var model = dte.ActiveDocument.ProjectItem.FileCodeModel;

            var decls = new List<TypeDecl> ();

            foreach (CodeElement e in model.CodeElements) {
                GetTopLevelTypeDecls (e, decls);
            }

            return decls.ToArray ();
        }

        protected override void MonitorEditorChanges ()
        {
            var dte = VisualStudio.ContinuousPackage.TheDTE;
            if (dte == null)
                throw new InvalidOperationException ("OMG Continuous Package has not inited yet");

            // throw new NotImplementedException ();
        }

        protected override async Task SetWatchTextAsync (WatchVariable w, List<string> vals)
        {
            // throw new NotImplementedException ();
        }

        class CodeTypeDecl : TypeDecl
        {
            public readonly CodeElement Element;
            readonly TextLoc startLoc, endLoc;
            public CodeTypeDecl (CodeElement elm)
            {
                Element = elm;
                startLoc = TextLocFromPoint (elm.StartPoint);
                endLoc = TextLocFromPoint (elm.EndPoint);
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
                    return Element.Name;
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
                throw new NotImplementedException ();
            }
        }
    }
}
#endif
