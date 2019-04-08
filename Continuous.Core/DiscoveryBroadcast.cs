using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Continuous
{
	public class DiscoveryBroadcast
	{
		public string DeviceName { get; set; }
		public string DeviceModel { get; set; }
		[Newtonsoft.Json.JsonIgnore]
		public string Id => $"{DeviceName} ({DeviceModel})";
		public DiscoveryBroadcastAddress[] Addresses { get; set; }

		public override bool Equals (object obj)
		{
			var o = obj as DiscoveryBroadcast;
			if (o == null) return false;
			if (DeviceName != o.DeviceName || DeviceModel != o.DeviceModel || Addresses.Length != o.Addresses.Length) return false;
			for (var i = 0; i < Addresses.Length; i++)
			{
				if (!Addresses[i].Equals (o.Addresses[i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode ()
		{
			var s = 1;
			if (Addresses != null)
			{
				foreach (var a in Addresses)
				{
					s += a.GetHashCode ();
				}
			}
			s += (DeviceName?.GetHashCode () + DeviceModel?.GetHashCode ()) ?? 0;
			return s;
		}

		public static DiscoveryBroadcast CreateForDevice (int port)
		{
			var allInterfaces = NetworkInterface.GetAllNetworkInterfaces ();
			var goodInterfaces =
				allInterfaces.Where (x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
									 !x.Name.StartsWith ("pdp_ip", StringComparison.Ordinal) &&
									 x.OperationalStatus == OperationalStatus.Up);
			var iips = goodInterfaces.SelectMany (x =>
				 x.GetIPProperties ().UnicastAddresses
				 .Where (y => y.Address.AddressFamily == AddressFamily.InterNetwork)
				 .Select (y => new DiscoveryBroadcastAddress
				 {
					 Address = y.Address.ToString (),
					 Port = port,
					 Interface = x.Name
				 }));
			var r = new DiscoveryBroadcast {
				DeviceName = "Device",
				DeviceModel = "Model",
				Addresses = iips.ToArray ()
			};
#if __IOS__
			var dev = UIKit.UIDevice.CurrentDevice;
			r.DeviceName = dev.Name;
			r.DeviceModel = dev.Model;
#endif
			return r;
		}
	}

	public class DiscoveryBroadcastAddress
	{
		public string Address { get; set; }
		public int Port { get; set; }
		public string Interface { get; set; }

		public override bool Equals (object obj)
		{
			var o = obj as DiscoveryBroadcastAddress;
			if (o == null) return false;
			if (Address != o.Address || Interface != o.Interface || Port != o.Port) return false;
			return true;
		}

		public override int GetHashCode ()
		{
			var s = 1;
			s += Address.GetHashCode () + Interface.GetHashCode () + Port.GetHashCode ();
			return s;
		}
	}
}
