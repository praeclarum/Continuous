using System;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace LiveCode.Server
{
	public class HttpServer
	{
		readonly Visualizer visualizer = new Visualizer ();

		HttpListener listener;
		TaskScheduler mainScheduler;

		readonly VM vm = new VM ();

		public void Run (int port = Http.DefaultPort)
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

					var req = JsonConvert.DeserializeObject<EvalRequest> (reqStr);

					var resp = await Task.Factory.StartNew (() => {
						var r = new EvalResponse ();
						try {
							r = vm.Eval (req.Code);
						}
						catch (Exception ex) {
							Log (ex, "vm.Eval");
						}
						try {
							Visualize (req, r);
						}
						catch (Exception ex) {
							Log (ex, "Visualize");
						}
						return Tuple.Create (r, JsonConvert.SerializeObject (r));
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

		void Visualize (EvalRequest req, EvalResponse resp)
		{
			if (!resp.HasResult) {
				return;
			}
			visualizer.Visualize (req, resp);
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

