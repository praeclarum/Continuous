using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace Continuous.Client
{
	public class HttpClient
	{
//		readonly Uri baseUrl;
		readonly Uri visualizeUrl;
		readonly Uri stopVisualizingUrl;
		readonly Uri watchChangesUrl;
		readonly WebClient client;

		public HttpClient (Uri baseUrl)
		{
//			this.baseUrl = baseUrl;
			visualizeUrl = new Uri (baseUrl, "visualize");
			stopVisualizingUrl = new Uri (baseUrl, "stopVisualizing");
			watchChangesUrl = new Uri (baseUrl, "watchChanges");
			client = new WebClient ();
		}

		public async Task<EvalResponse> VisualizeAsync (string code)
		{
			var req = new EvalRequest { Code = code };

			var reqStr = JsonConvert.SerializeObject (req);

			var respStr = await client.UploadStringTaskAsync (visualizeUrl, reqStr);

			return JsonConvert.DeserializeObject<EvalResponse> (respStr);
		}

		public async Task StopVisualizingAsync ()
		{
			await client.UploadStringTaskAsync (stopVisualizingUrl, "");
		}

		public async Task<WatchValuesResponse> WatchChangesAsync (long version)
		{
//			Console.WriteLine ("WC " + version);

			var req = new WatchChangesRequest { Version = version };

			var reqStr = JsonConvert.SerializeObject (req);

			var respStr = await client.UploadStringTaskAsync (watchChangesUrl, reqStr);

			return JsonConvert.DeserializeObject<WatchValuesResponse> (respStr);
		}
	}
}

