using System;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;

namespace LiveCode.Server
{
	public partial class Visualizer
	{
		public Visualizer ()
		{
			PlatformInitialize ();
		}

		public void Visualize (EvalRequest req, EvalResponse resp)
		{
			PlatformVisualize (req, resp);
		}

		partial void PlatformInitialize ();
		partial void PlatformVisualize (EvalRequest req, EvalResponse resp);
	}

}

