using System;
using System.Linq;

namespace LiveCode
{
	public static class Http
	{
		public const int DefaultPort = 9634;
	}

	public class EvalRequest
	{
		public string Code;
	}

	public class EvalMessage
	{
		public string MessageType;
		public string Text;
		public int Line;
		public int Column;
	}

	public class EvalResponse
	{
		public EvalMessage[] Messages;
		public TimeSpan Duration;
		public object Result;
		public bool HasResult;

		public bool HasErrors {
			get { return Messages.Any (m => m.MessageType == "error"); }
		}
	}

}

