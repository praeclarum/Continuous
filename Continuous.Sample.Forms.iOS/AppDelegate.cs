using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace Continuous.Sample.Forms.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init ();

			LoadApplication (new App ());

			new Continuous.Server.HttpServer ().Run ();

			return base.FinishedLaunching (app, options);
		}
	}
}

