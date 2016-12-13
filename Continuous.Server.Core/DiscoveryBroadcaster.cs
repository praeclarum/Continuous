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
		readonly UdpClient client = new UdpClient (Http.DiscoveryBroadcastPort)
		{
			EnableBroadcast = true
		};

		IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, Http.DiscoveryBroadcastPort);

		Timer timer = new Timer(3000);

		public DiscoveryBroadcaster (string name = "")
		{
			timer.Elapsed += Timer_Elapsed;
			timer.Start ();
		}

		void Timer_Elapsed (object sender, ElapsedEventArgs e)
		{
			try
			{
				var broadcast = DiscoveryBroadcast.CreateForDevice ();
				var json = Newtonsoft.Json.JsonConvert.SerializeObject (broadcast, Newtonsoft.Json.Formatting.None);
				var bytes = System.Text.Encoding.UTF8.GetBytes (json);

				client.Send (bytes, bytes.Length, broadcastEndpoint);
				Debug.WriteLine ($"BROADCAST {json}");
			}
			catch (Exception ex)
			{
				Console.WriteLine ("FAILED TO BROADCAST " + ex);
			}
		}
	}
}
