using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Views;
using Android.Content;

namespace LiveCode.Server
{
	[Activity (Label = "ObjectInspector")]
	public class ObjectInspector : ListActivity
	{
		class DataAdapter : BaseAdapter
		{
			readonly ObjectInspectorData data;
			readonly Context context;
			public DataAdapter (ObjectInspectorData data, Context context)
			{
				this.data = data;
				this.context = context;
			}

			public override Java.Lang.Object GetItem (int position)
			{
				return null;
			}

			public override long GetItemId (int position)
			{
				return 0;
			}

			public override View GetView (int position, View convertView, ViewGroup parent)
			{
				var v = convertView as TextView;
				if (v == null) {
					v = new TextView (context);
				}

				if (position == 0) {
					v.Text = data.ToStringValue;
				} else if (position == 1) {
					v.Text = data.HashDisplayString;
				} else {
					var i = position - 2;
					v.Text = data.Properties[i].Name + " = " + data.Properties[i].ValueString;
				}

				return v;
			}

			public override int Count {
				get {
					return 2 + data.Properties.Length;
				}
			}
		}

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			var objectKey = Intent.GetStringExtra ("objectKey");
			var target = GetKeyedObject (objectKey);

			var data = new ObjectInspectorData (target);

			ListAdapter = new DataAdapter (data, this);

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

