using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Timers;

using Continuous;

namespace Continuous.Server
{
	public class DiscoveryBroadcaster
	{
		readonly IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, Http.DiscoveryBroadcastReceiverPort);
		readonly int httpPort;

		UdpClient client;
		Timer timer = new Timer(3000);

		public DiscoveryBroadcaster (int httpPort = 0)
		{
			timer.Elapsed += Timer_Elapsed;
			timer.Start ();
			this.httpPort = (httpPort == 0) ? Http.DefaultPort : httpPort;
		}

		void Timer_Elapsed (object sender, ElapsedEventArgs e)
		{
			try
			{
				if (client == null)
				{
					client = new UdpClient (Http.DiscoveryBroadcastPort)
					{
						EnableBroadcast = true
					};
				}

				var broadcast = DiscoveryBroadcast.CreateForDevice (httpPort);
				var json = Newtonsoft.Json.JsonConvert.SerializeObject (broadcast, Newtonsoft.Json.Formatting.None);
				var bytes = System.Text.Encoding.UTF8.GetBytes (json);

				client.Send (bytes, bytes.Length, broadcastEndpoint);
				//Debug.WriteLine ($"BROADCAST {json}");
			}
			catch (Exception ex)
			{
				Debug.WriteLine ($"FAILED TO BROADCAST ON PORT {Http.DiscoveryBroadcastPort}: {ex}");
			}
		}
	}
}
