using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace LiveCode.Client
{
	public class HttpClient
	{
		readonly Uri baseUrl;
		readonly WebClient client;

		public HttpClient (Uri baseUrl)
		{
			this.baseUrl = baseUrl;
			client = new WebClient ();
		}

		public async Task<EvalResponse> VisualizeAsync (string code)
		{
			var req = new EvalRequest { Code = code };

			var reqStr = JsonConvert.SerializeObject (req);

			var respStr = await client.UploadStringTaskAsync (baseUrl, reqStr);

			return JsonConvert.DeserializeObject<EvalResponse> (respStr);
		}
	}
}

