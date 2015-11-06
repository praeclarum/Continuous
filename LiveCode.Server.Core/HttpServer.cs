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
					Console.WriteLine (ex);
				}
			}
		}

		async void HandleRequest (HttpListenerContext c)
		{
			try {

				Console.WriteLine ("REQ ON THREAD {0}", Thread.CurrentThread.ManagedThreadId);

				var reqStr = await new StreamReader (c.Request.InputStream, Encoding.UTF8).ReadToEndAsync ().ConfigureAwait (false);

				var req = JsonConvert.DeserializeObject<EvalRequest> (reqStr);

				var resp = await Task.Factory.StartNew (() => {
					return vm.Eval (req.Code);
				}, CancellationToken.None, TaskCreationOptions.None, mainScheduler);

				var respStr = JsonConvert.SerializeObject (resp);

				Console.WriteLine (respStr);

				var bytes = Encoding.UTF8.GetBytes (respStr);
				c.Response.StatusCode = 200;
				c.Response.ContentLength64 = bytes.LongLength;
				await c.Response.OutputStream.WriteAsync (bytes, 0, bytes.Length).ConfigureAwait (false);

			} catch (Exception ex) {
				Console.WriteLine (ex);
				c.Response.StatusCode = 500;
			} finally {
				c.Response.Close ();
			}
		}

	}
}

