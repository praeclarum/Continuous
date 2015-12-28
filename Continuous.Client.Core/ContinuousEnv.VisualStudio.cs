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

        protected override Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ()
        {
            throw new NotImplementedException ();
        }

        protected override void MonitorEditorChanges ()
        {
            // throw new NotImplementedException ();
        }

        protected override async Task SetWatchTextAsync (WatchVariable w, List<string> vals)
        {
            // throw new NotImplementedException ();
        }
    }
}
#endif
