using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

#if VISUALSTUDIO
using EnvDTE;

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
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "Continuous Coding");
        }

        protected override Task<TypeDecl> FindTypeAtCursorAsync ()
        {
            throw new NotImplementedException ();
        }

        protected override Task<string> GetSelectedTextAsync ()
        {
            throw new NotImplementedException ();
        }

        protected override Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ()
        {
            throw new NotImplementedException ();
        }

        protected override void MonitorEditorChanges ()
        {
        }

        protected override async Task SetWatchTextAsync (WatchVariable w, List<string> vals)
        {
        }
    }
}
#endif
