using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;

namespace Continuous.Client.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(MainPad))]
    [Guid(ContinuousPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class ContinuousPackage : Package
    {
        public const string PackageGuidString = "e0ec91a1-5c4d-4053-a6e0-ac8e489213c7";

        /// <summary>
        /// How the hell is a UI control supposed to get access to the DTE without terrible hacks like this?
        /// </summary>
        public static DTE TheDTE;

        protected override void Initialize()
        {
            TheDTE = (DTE)GetService(typeof(DTE));
            MainPadCommand.Initialize(this);
            base.Initialize();
        }

    }
}
