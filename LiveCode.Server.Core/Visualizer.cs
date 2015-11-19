using System;

namespace LiveCode.Server
{
	public partial class Visualizer
	{
		readonly object context;

		public Visualizer (object context)
		{
			this.context = context;
			PlatformInitialize ();
		}

		public void Visualize (EvalRequest req, EvalResponse resp)
		{
			PlatformVisualize (req, resp);
		}

		public void StopVisualizing ()
		{
			PlatformStopVisualizing ();
		}

		partial void PlatformInitialize ();
		partial void PlatformVisualize (EvalRequest req, EvalResponse resp);
		partial void PlatformStopVisualizing ();

		void Log (string format, params object[] args)
		{
			#if DEBUG
			Log (string.Format (format, args));
			#endif
		}

		void Log (string msg)
		{
			#if DEBUG
			System.Diagnostics.Debug.WriteLine (msg);
			#endif
		}
	}

}

