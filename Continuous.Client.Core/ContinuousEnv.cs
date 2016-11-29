using System;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Continuous.Client
{
	public abstract partial class ContinuousEnv
	{
		public static ContinuousEnv Shared;

        static ContinuousEnv()
        {
            SetSharedPlatformEnvImpl ();
        }

		public ContinuousEnv ()
		{
			IP = Http.DefaultHost;
			Port = Http.DefaultPort;
		}

        static partial void SetSharedPlatformEnvImpl ();

        public string MonitorTypeName = "";

		HttpClient conn = null;
		void Connect ()
		{
			if (conn == null || conn.BaseUrl != ServerUrl) {
				conn = CreateConnection ();
			}
		}

		public string IP { get; set; }
		public int Port { get; set; }

		Uri ServerUrl {
			get {
				return new Uri ("http://" + IP.Trim () + ":" + Port);
			}
		}

		protected HttpClient CreateConnection ()
		{
			return new HttpClient (ServerUrl);
		}

		public event Action<string> Alerted;

        public void Alert (string format, params object[] args)
        {
			OnAlert (format, args);
        }

		protected virtual void OnAlert (string format, params object[] args)
		{
			Log (format, args);

			var a = Alerted;
			if (a != null)
			{
				var m = string.Format (System.Globalization.CultureInfo.CurrentUICulture, format, args);
				a (m);
			}
		}

		protected async Task<EvalResponse> EvalForResponseAsync (string declarations, string valueExpression, bool showError)
		{
			Connect ();
			var r = await conn.VisualizeAsync (declarations, valueExpression);
			var err = r.HasErrors;
			if (err) {
				var message = string.Join ("\n", r.Messages.Select (m => m.MessageType + ": " + m.Text));
				if (showError) {
					Alert ("{0}", message);
				}
			}
			return r;
		}

        public async Task VisualizeAsync ()
        {
            var typedecl = await FindTypeAtCursorAsync ();

            if (typedecl == null) {
                Alert ("Could not find a type at the cursor.");
                return;
            }

            EnsureMonitoring ();

            var typeName = typedecl.Name;

            MonitorTypeName = typeName;
            //			monitorNamespace = nsName;

            await SetTypesAndVisualizeMonitoredTypeAsync (forceEval: true, showError: true);
        }

        protected async Task SetTypesAndVisualizeMonitoredTypeAsync (bool forceEval, bool showError)
        {
            //
            // Gobble up all we can about the types in the active document
            //
            var typeDecls = await GetTopLevelTypeDeclsAsync ();
            foreach (var td in typeDecls) {
                td.SetTypeCode ();
            }

            await VisualizeMonitoredTypeAsync (forceEval, showError);
        }

        bool monitoring = false;
        void EnsureMonitoring ()
        {
            if (monitoring) return;

            MonitorEditorChanges ();
            MonitorWatchChanges ();

            monitoring = true;
        }

        protected abstract void MonitorEditorChanges ();

        protected abstract Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ();

        async Task<TypeDecl> FindTypeAtCursorAsync ()
        {
            var editLoc = await GetCursorLocationAsync ();
            if (!editLoc.HasValue)
                return null;
            var editTLoc = editLoc.Value;

            var selTypeDecl =
                (await GetTopLevelTypeDeclsAsync ()).
                FirstOrDefault (x => x.StartLocation <= editTLoc && editTLoc <= x.EndLocation);
            return selTypeDecl;
        }

        protected abstract Task<TextLoc?> GetCursorLocationAsync ();

        LinkedCode lastLinkedCode = null;

        public async Task VisualizeMonitoredTypeAsync (bool forceEval, bool showError)
        {
            //
            // Refresh the monitored type
            //
            if (string.IsNullOrWhiteSpace (MonitorTypeName))
                return;

            var monitorTC = TypeCode.Get (MonitorTypeName);

            var code = await Task.Run (() => monitorTC.GetLinkedCode ());

            OnLinkedMonitoredCode (code);

            if (!forceEval && lastLinkedCode != null && lastLinkedCode.CacheKey == code.CacheKey) {
                return;
            }

            //
            // Send the code to the device
            //
            try {
                //
                // Declare and Show it
                //
                Log (code.ValueExpression);
				var resp = await EvalForResponseAsync (code.Declarations, code.ValueExpression, showError);
                if (resp.HasErrors)
                    return;

                //
                // If we made it this far, remember so we don't re-send the same
                // thing immediately
                //
                lastLinkedCode = code;

                //
                // Update the editor
                //
                await UpdateEditorAsync (code, resp);

            }
            catch (Exception ex) {
                if (showError) {
                    Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
                }
            }
        }

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

        async Task UpdateEditorAsync (LinkedCode code, EvalResponse resp)
        {
            await UpdateEditorWatchesAsync (code.Types.SelectMany (x => x.Watches), resp.WatchValues);
        }

        List<WatchVariable> lastWatches = new List<WatchVariable> ();

        async Task UpdateEditorWatchesAsync (WatchValuesResponse watchValues)
        {
            await UpdateEditorWatchesAsync (lastWatches, watchValues.WatchValues);
        }

        async Task UpdateEditorWatchesAsync (IEnumerable<WatchVariable> watches, Dictionary<string, List<string>> watchValues)
        {
            var ws = watches.ToList ();
            foreach (var w in ws) {
                List<string> vals;
                if (!watchValues.TryGetValue (w.Id, out vals)) {
                    vals = new List<string> ();
                }
                //				Console.WriteLine ("VAL {0} {1} = {2}", w.Id, w.Expression, vals);
                await SetWatchTextAsync (w, vals);
            }
            lastWatches = ws;
        }

        protected abstract Task SetWatchTextAsync (WatchVariable w, List<string> vals);

        protected string GetValsText (List<string> vals)
        {
            var maxLength = 72;
            var newText = string.Join (", ", vals);
            newText = newText.Replace ("\r\n", " ").Replace ("\n", " ").Replace ("\t", " ");
            if (newText.Length > maxLength) {
                newText = "..." + newText.Substring (newText.Length - maxLength);
            }
            return newText;
        }

        async void MonitorWatchChanges ()
        {
            var version = 0L;
            var conn = CreateConnection ();
            for (;;) {
                try {
                    //					Console.WriteLine ("MON WATCH " + DateTime.Now);
                    var res = await conn.WatchChangesAsync (version);
                    if (res != null) {
                        version = res.Version;
                        await UpdateEditorWatchesAsync (res);
                    }
                    else {
                        await Task.Delay (1000);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine (ex);
                    await Task.Delay (3000);
                }
            }
        }



        public event Action<LinkedCode> LinkedMonitoredCode = delegate {};

		protected void OnLinkedMonitoredCode (LinkedCode code)
		{
			LinkedMonitoredCode (code);
		}

        protected abstract Task<string> GetSelectedTextAsync ();

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

