using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Android.App;
using Android.Views;

namespace LiveCode.Server
{
	public partial class Visualizer
	{
		partial void PlatformStopVisualizing ()
		{
		}

		partial void PlatformVisualize (EvalRequest req, EvalResponse resp)
		{
			var val = resp.Result;
			var ty = val != null ? val.GetType () : typeof(object);

			Log ("{0} value = {1}", ty.FullName, val);

			ShowViewerAsync (GetViewer (req, resp)).ContinueWith (t => {
				if (t.IsFaulted) {
					Log ("ShowViewer ERROR {0}", t.Exception);
				}
			});
		}

		object GetViewer (EvalRequest req, EvalResponse resp)
		{
			return resp.Result;
		}

		async Task ShowViewerAsync (object obj)
		{
			var c = context as global::Android.Content.Context;
			if (c == null)
				return;
			var key = Guid.NewGuid ().ToString ();
			ObjectInspector.SetKeyedObject (key, obj);
			var intent = new global::Android.Content.Intent (c, typeof (ObjectInspector));
			intent.PutExtra ("objectKey", key);
			c.StartActivity (intent);
		}
	}
}

