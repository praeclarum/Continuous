using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Continuous.Server
{
	public partial class HttpServer
	{
		readonly int port;
		readonly Visualizer visualizer;

		HttpListener listener;
		TaskScheduler mainScheduler;

		readonly IVM vm;

		public HttpServer (object context = null, int port = Http.DefaultPort, IVM vm = null)
		{
			this.port = port;
			visualizer = new Visualizer (context);
			this.vm = vm ?? (new VM());
		}

		public void Run ()
		{
			mainScheduler = TaskScheduler.FromCurrentSynchronizationContext ();

			Task.Run (() => {
				var url = "http://+:" + port + "/";

				var remTries = 2;

				while (remTries > 0) {
					remTries--;

					listener = new HttpListener ();
					listener.Prefixes.Add (url);

					try {
						listener.Start ();
						remTries = 0;
					} catch (HttpListenerException ex) {
						if (remTries == 1 && ex.ErrorCode == 5) { // Access Denied
							GrantServerPermission (url);
						} else {
							throw;
						}
					}
				}

				Loop ();
			});
		}

        partial void GrantServerPermission (string url);

		// Analysis disable once FunctionNeverReturns
		async void Loop ()
		{
			for (;;) {
				var c = await listener.GetContextAsync ().ConfigureAwait (false);
				try {
					HandleRequest (c);					
				} catch (Exception ex) {
					Log (ex, "HandleRequest");
				}
			}
		}

		async void HandleRequest (HttpListenerContext c)
		{
			try {

				var path = c.Request.Url.AbsolutePath;
				var resString = "";

				Log ("REQ ON THREAD {0} {1}", Thread.CurrentThread.ManagedThreadId, path);

				if (path == "/watchChanges") {
					try {
						var reqStr = await new StreamReader (c.Request.InputStream, Encoding.UTF8).ReadToEndAsync ().ConfigureAwait (false);
						var req = JsonConvert.DeserializeObject<WatchChangesRequest> (reqStr);
						resString = JsonConvert.SerializeObject (await GetWatchChangesAsync (req));
					} catch (Exception ex) {
						Log (ex, "/watchChanges");
					}
				} else if (path == "/stopVisualizing") {
					try {
						resString = await Task.Factory.StartNew (() => {
							visualizer.StopVisualizing ();
							return "";
						}, CancellationToken.None, TaskCreationOptions.None, mainScheduler);
					} catch (Exception ex) {
						Log (ex, "/stopVisualizing");
					}
				}
				else {
					var reqStr = await new StreamReader (c.Request.InputStream, Encoding.UTF8).ReadToEndAsync ().ConfigureAwait (false);

//					Log (reqStr);

					var req = JsonConvert.DeserializeObject<EvalRequest> (reqStr);

					var token = CancellationToken.None;

					var resp = await Task.Factory.StartNew (() => {
						WatchStore.Clear ();
						var r = new EvalResult ();
						try {
							r = vm.Eval (req, mainScheduler, token);
						}
						catch (Exception ex) {
							Log (ex, "vm.Eval");
						}
						try {
							Task.Factory.StartNew (() =>
							{
								Visualize (r);
							}, token, TaskCreationOptions.None, mainScheduler).Wait ();
						}
						catch (Exception ex) {
							Log (ex, "Visualize");
						}
						var response = new EvalResponse {
							Messages = r.Messages,
							WatchValues = WatchStore.Values,
							Duration = r.Duration,
						};
						return Tuple.Create (r, JsonConvert.SerializeObject (response));
					}, token);

					Log (resp.Item2);

					resString = resp.Item2;
				}

				var bytes = Encoding.UTF8.GetBytes (resString);
				c.Response.StatusCode = 200;
				c.Response.ContentLength64 = bytes.LongLength;
				await c.Response.OutputStream.WriteAsync (bytes, 0, bytes.Length).ConfigureAwait (false);

			} catch (Exception ex) {
				Log (ex, "HandleRequest");
				c.Response.StatusCode = 500;
			} finally {
				c.Response.Close ();
			}
		}

		Task<WatchValuesResponse> GetWatchChangesAsync (WatchChangesRequest req)
		{
			var tcs = new TaskCompletionSource<WatchValuesResponse> ();
			Action setResult = () => {
				tcs.SetResult (new WatchValuesResponse {
					WatchValues = new Dictionary<string, List<string>> (WatchStore.Values),
					Version = WatchStore.Version,
				});
			};
			if (WatchStore.Version > req.Version) {
				setResult ();
			} else {
				EventHandler handle = null;
				handle = (s, e) => {
					if (WatchStore.Version > req.Version) {
						WatchStore.Recorded -= handle;
						setResult ();
					}
				};
				WatchStore.Recorded += handle;
			}
			return tcs.Task;
		}

		void Visualize (EvalResult res)
		{
			if (!res.HasResult) {
				return;
			}
			try {
				Log ("Continuous.Visualize: {0}", res.Result);
			// Analysis disable once EmptyGeneralCatchClause
			} catch (Exception) {				
			}
			visualizer.Visualize (res);
		}

		void Log (Exception ex, string env)
		{
			Console.WriteLine ("ERROR IN " + env);
			Console.WriteLine ("{0}", ex);
		}

		void Log (string format, params object[] args)
		{
			Log (string.Format (format, args));
		}

		void Log (string msg)
		{
			#if DEBUG
			Console.WriteLine (msg);
			#endif
		}
	}
}

