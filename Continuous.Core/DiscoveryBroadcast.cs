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

		public static DiscoveryBroadcast CreateForDevice ()
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
		public string Interface { get; set; }
	}
}
