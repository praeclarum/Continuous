using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Continuous.Server
{
	public class HttpServer
	{
		readonly int port;
		readonly Visualizer visualizer;

		HttpListener listener;
		TaskScheduler mainScheduler;

		readonly VM vm = new VM ();

		public HttpServer (object context = null, int port = Http.DefaultPort)
		{
			this.port = port;
			visualizer = new Visualizer (context);
		}

		public void Run ()
		{
			mainScheduler = TaskScheduler.FromCurrentSynchronizationContext ();

			Task.Run (() => {
				listener = new HttpListener ();
				listener.Prefixes.Add ("http://127.0.0.1:" + port + "/");
				listener.Start ();
				Loop ();
			});
		}

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

				if (path == "/stopVisualizing") {
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

					var resp = await Task.Factory.StartNew (() => {
						var r = new EvalResult ();
						try {
							r = vm.Eval (req.Code);
						}
						catch (Exception ex) {
							Log (ex, "vm.Eval");
						}
						try {
							Visualize (r);
						}
						catch (Exception ex) {
							Log (ex, "Visualize");
						}
						var response = new EvalResponse {
							Messages = r.Messages,
							Duration = r.Duration
						};
						return Tuple.Create (r, JsonConvert.SerializeObject (response));
					}, CancellationToken.None, TaskCreationOptions.None, mainScheduler);

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

		void Visualize (EvalResult res)
		{
			if (!res.HasResult) {
				return;
			}
			try {
				Console.WriteLine ("Continuous.Visualize: {0}", res.Result);
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

