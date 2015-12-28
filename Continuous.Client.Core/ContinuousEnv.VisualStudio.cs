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
        public override Task VisualizeAsync ()
        {
            throw new NotImplementedException ();
        }

        public override Task VisualizeMonitoredTypeAsync (bool forceEval, bool showError)
        {
            throw new NotImplementedException ();
        }

        public override Task VisualizeSelectionAsync ()
        {
            throw new NotImplementedException ();
        }

        protected override void AlertImpl(string format, params object[] args)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "Continuous Coding");
        }
    }
}
#endif
