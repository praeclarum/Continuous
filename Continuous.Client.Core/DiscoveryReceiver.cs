using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Continuous.Client
{
	public class DiscoveryReceiver
	{
		readonly UdpClient listener;

		readonly Dictionary<string, DiscoveryBroadcast> devices =
			new Dictionary<string, DiscoveryBroadcast>();

		Thread thread;
		bool running = true;

		public event EventHandler DevicesChanged;

		public DiscoveryReceiver ()
		{
			try
			{
				listener = new UdpClient (Http.DiscoveryBroadcastReceiverPort, AddressFamily.InterNetwork);
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Continuous: Failed to listen: " + ex);
				listener = null;
			}
			if (listener != null)
			{
				thread = new Thread (Run);
				thread.Start ();
			}
		}

		public string[] Devices
		{
			get
			{
				lock (devices)
				{
					return devices.Keys.ToArray ();
				}
			}
		}

		public string Resolve (string device)
		{
			lock (devices)
			{
				DiscoveryBroadcast b;
				if (devices.TryGetValue (device, out b) && b.Addresses.Length > 0)
				{
					return b.Addresses[0].Address;
				}
				return device;
			}
		}

		public void Stop ()
		{
			running = false;
			thread = null;
		}

		void Run ()
		{
			while (running)
			{
				try
				{
					Listen ();
				}
				catch (Exception ex)
				{
					Debug.WriteLine ("DISCOVERY RECEIVE FAILED " + ex);
				}
			}
		}

		void Listen ()
		{
			var broadcastEndpoint = new IPEndPoint(IPAddress.Any, Http.DiscoveryBroadcastReceiverPort);

			var bytes = listener.Receive (ref broadcastEndpoint);

			var json = System.Text.Encoding.UTF8.GetString (bytes);

			var newBroadcast = Newtonsoft.Json.JsonConvert.DeserializeObject<DiscoveryBroadcast> (json);

			bool changed = false;
			lock (devices)
			{
				var id = newBroadcast.Id;
				DiscoveryBroadcast oldBroadcast;
				if (devices.TryGetValue (id, out oldBroadcast))
				{
					if (!oldBroadcast.Equals (newBroadcast))
					{
						changed = true;
						devices[id] = newBroadcast;
					}
				}
				else
				{
					changed = true;
					devices[id] = newBroadcast;
				}
			}
			if (changed)
			{
				DevicesChanged?.Invoke (this, EventArgs.Empty);
			}
		}
	}
}
