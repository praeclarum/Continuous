using System;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Continuous.Client
{
	public abstract partial class ContinuousEnv
	{
        // o_O partial f-ing methods
		public static ContinuousEnv Shared;

        static ContinuousEnv()
        {
            SetSharedPlatformEnvImpl ();
        }

        static partial void SetSharedPlatformEnvImpl ();

        public string MonitorTypeName = "";

		HttpClient conn = null;
		void Connect ()
		{
			if (conn == null) {
				conn = CreateConnection ();
			}
		}

		protected HttpClient CreateConnection ()
		{
			return new HttpClient (new Uri ("http://127.0.0.1:" + Http.DefaultPort));
		}

        public void Alert(string format, params object[] args)
        {
            Log(format, args);
            AlertImpl(format, args);
        }

        protected abstract void AlertImpl (string format, params object[] args);

        protected async Task<bool> EvalAsync (string code, bool showError)
		{
			var r = await EvalForResponseAsync (code, showError);
			var err = r.HasErrors;
			return !err;
		}

		protected async Task<EvalResponse> EvalForResponseAsync (string code, bool showError)
		{
			Connect ();
			var r = await conn.VisualizeAsync (code);
			var err = r.HasErrors;
			if (err) {
				var message = string.Join ("\n", r.Messages.Select (m => m.MessageType + ": " + m.Text));
				if (showError) {
					Alert ("{0}", message);
				}
			}
			return r;
		}

        public abstract Task VisualizeSelectionAsync ();

        public abstract Task VisualizeAsync ();

		public abstract Task VisualizeMonitoredTypeAsync (bool forceEval, bool showError);

		public async Task StopVisualizingAsync ()
		{
			MonitorTypeName = "";
			TypeCode.Clear ();
			try {
				Connect ();
				await conn.StopVisualizingAsync ();
			} catch (Exception ex) {
				Log ("ERROR: {0}", ex);
			}
		}

		public event Action<LinkedCode> LinkedMonitoredCode = delegate {};

		protected void OnLinkedMonitoredCode (LinkedCode code)
		{
			LinkedMonitoredCode (code);
		}

		protected void Log (string format, params object[] args)
		{
#if DEBUG
			Log (string.Format (format, args));
#endif
		}

		protected void Log (string msg)
		{
#if DEBUG
			Console.WriteLine (msg);
#endif
		}

		protected void Log (Exception ex)
		{
#if DEBUG
			Console.WriteLine (ex.ToString ());
#endif
		}
	}
}

