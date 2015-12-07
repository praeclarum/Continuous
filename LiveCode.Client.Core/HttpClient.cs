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
		readonly WebClient client;

		public HttpClient (Uri baseUrl)
		{
//			this.baseUrl = baseUrl;
			visualizeUrl = new Uri (baseUrl, "visualize");
			stopVisualizingUrl = new Uri (baseUrl, "stopVisualizing");
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
	}
}

