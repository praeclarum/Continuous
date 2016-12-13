using System;
using System.Linq;
using System.Collections.Generic;

namespace Continuous
{
	public static class Http
	{
		public const string DefaultHost = "127.0.0.1";
		public const int DefaultPort = 9634;
		public const int DiscoveryBroadcastPort = 9636;
		public const int DiscoveryBroadcastReceiverPort = 9637;
	}

	public class EvalRequest
	{
		public string Declarations;
		public string ValueExpression;
	}

	public class EvalMessage
	{
		public string MessageType;
		public string Text;
		public int Line;
		public int Column;
	}

	public class EvalResult
	{
		public EvalMessage[] Messages;
		public TimeSpan Duration;
		public object Result;
		public bool HasResult;

		public bool HasErrors {
			get { return Messages != null && Messages.Any (m => m.MessageType == "error"); }
		}
	}

	public class EvalResponse
	{
		public EvalMessage[] Messages;
		public Dictionary<string, List<string>> WatchValues;
		public TimeSpan Duration;

		public bool HasErrors {
			get { return Messages != null && Messages.Any (m => m.MessageType == "error"); }
		}
	}

	public class WatchChangesRequest
	{
		public long Version;
	}

	public class WatchValuesResponse
	{
		public Dictionary<string, List<string>> WatchValues;
		public long Version;
	}
}

