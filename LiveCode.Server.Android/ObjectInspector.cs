using System;
using Android.App;
using Android.OS;
using System.Collections.Generic;

namespace LiveCode.Server
{
	[Activity (Label = "ObjectInspector")]
	public class ObjectInspector : ListActivity
	{
		object target;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			var objectKey = Intent.GetStringExtra ("objectKey");
			target = GetKeyedObject (objectKey);

			Title = string.Format ("{0}", target);
		}

		static readonly Dictionary<string, object> keyedObjects = new Dictionary<string, object> ();

		public static void SetKeyedObject (string key, object obj)
		{
			keyedObjects [key] = obj;
		}

		public static object GetKeyedObject (string key)
		{
			object r;
			keyedObjects.TryGetValue (key ?? "", out r);
			return r;
		}

	}
}

