using System;
using System.Collections.Generic;

namespace Continuous.Server
{
	public static class WatchStore
	{		
		const int MaxValuesPerVariable = 100;
		static readonly Dictionary<string, List<string>> watchValues = new Dictionary<string, List<string>> ();

		public static event EventHandler Recorded = delegate{};

		public static void Clear ()
		{
			watchValues.Clear ();
		}

		static long version = 0;

		public static long Version {
			get {
				return version;
			}
		}

		public static void Record (string id, object value)
		{
			try {

				List<string> vals;
				if (!watchValues.TryGetValue (id, out vals)) {
					vals = new List<string> ();
					watchValues.Add (id, vals);
				}

				if (vals.Count < MaxValuesPerVariable) {
					vals.Add (GetString (value));
				}

				version = DateTime.UtcNow.Ticks;
				Recorded (null, EventArgs.Empty);

			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		public static Dictionary<string, List<string>> Values {
			get { return watchValues; }
		}

		static string GetString (object value)
		{
			if (value == null)
				return "null";

			var typeName = value.GetType ().FullName;
			var r = typeName;
			try {
				r = value.ToString ();
			} catch (Exception ex) {
				r = ex.GetType ().Name + ": " + ex.Message;
			}

			return r;
		}

	}
}

