using System;
using Android.App;
using Android.OS;
using System.Collections.Generic;

namespace LiveCode.Server
{
	[Activity (Label = "ObjectInspector")]
	public class ObjectInspector : ListActivity
	{
		ObjectInspectorData data = new ObjectInspectorData ();

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			var objectKey = Intent.GetStringExtra ("objectKey");
			var target = GetKeyedObject (objectKey);

			data = new ObjectInspectorData (target);

			Title = data.Title;
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

