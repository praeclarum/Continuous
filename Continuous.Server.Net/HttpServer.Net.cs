using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Continuous.Server
{
    public partial class HttpServer
    {
        partial void GrantServerPermission (string url)
        {
            var args = string.Format ("http add urlacl url={0} user={1}", url, System.Environment.UserDomainName);
            var p = new System.Diagnostics.Process ();
            p.StartInfo.FileName = "netsh";
            p.StartInfo.Arguments = args;
            p.StartInfo.Verb = "runas";
            p.Start ();
            p.WaitForExit ();
        }
    }
}
