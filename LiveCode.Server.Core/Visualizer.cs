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

		public void Visualize (EvalResult res)
		{
			PlatformVisualize (res);
		}

		public void StopVisualizing ()
		{
			PlatformStopVisualizing ();
		}

		partial void PlatformInitialize ();
		partial void PlatformVisualize (EvalResult res);
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

