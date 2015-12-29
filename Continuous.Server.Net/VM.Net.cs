using System;
using Mono.CSharp;

namespace Continuous.Server
{
    public partial class VM
    {
        partial void PlatformSettings (CompilerSettings settings)
        {
        }

        partial void PlatformInit ()
        {
            object res;
            bool hasRes;
            eval.Evaluate ("using System.Windows.Forms;", out res, out hasRes);
        }
    }
}

