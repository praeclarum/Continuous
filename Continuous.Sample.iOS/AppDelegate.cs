using System;

using Foundation;
using UIKit;

using Continuous.Server;
using System.Threading.Tasks;
using System.Threading;

namespace Continuous.Sample.iOS
{
	[Register ("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		public override UIWindow Window {
			get;
			set;
		}

		public override bool FinishedLaunching (UIApplication application, NSDictionary launchOptions)
		{
			new Continuous.Server.HttpServer ().Run ();

			Window = new UIWindow (UIScreen.MainScreen.Bounds);
			Window.RootViewController = new UIViewController ();
			Window.MakeKeyAndVisible ();
			return true;
		}

		class TestVM : IVM
		{
			public EvalResult Eval(EvalRequest code, TaskScheduler mainScheduler, CancellationToken token)
			{
				var t = Task.Factory.StartNew(() => {
					var r = new EvalResult();
					r.HasResult = true;
					r.Result = new UILabel
					{
						BackgroundColor = UIColor.Red,
						Text = "Hello at " + DateTime.Now,
					};
					return r;
				}, token, TaskCreationOptions.None, mainScheduler);
				return t.Result;
			}
		}

		public override void OnResignActivation (UIApplication application)
		{
		}

		public override void DidEnterBackground (UIApplication application)
		{
		}

		public override void WillEnterForeground (UIApplication application)
		{
		}

		public override void OnActivated (UIApplication application)
		{
		}

		public override void WillTerminate (UIApplication application)
		{
		}
	}
}


