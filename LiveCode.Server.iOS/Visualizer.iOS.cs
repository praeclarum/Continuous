using System;
using UIKit;

namespace LiveCode.Server
{
	public partial class Visualizer
	{
		partial void PlatformVisualize (EvalRequest req, EvalResponse resp)
		{
			var val = resp.Result;
			var ty = val != null ? val.GetType () : typeof(object);

			Console.WriteLine ("{0} value = {1}", ty.FullName, val);

			ShowViewer (GetViewer (req, resp));
		}

		UIViewController GetViewer (EvalRequest req, EvalResponse resp)
		{
			var vc = new UIViewController ();
			var tv = new UITextView ();
			vc.View = tv;
			tv.Text = resp.Result.ToString ();
			return vc;
		}

		UINavigationController presentedNav = null;

		void ShowViewer (UIViewController vc)
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			if (window == null)
				return;
			var rootVC = window.RootViewController;
			if (rootVC == null)
				return;

			vc.NavigationItem.RightBarButtonItem = new UIBarButtonItem (
				UIBarButtonSystemItem.Done,
				(_, __) => rootVC.DismissViewController (true, null));

			//
			// Try to just swap out the root VC if we've already presented
			//
			if (presentedNav != null && rootVC.PresentedViewController == presentedNav) {
				presentedNav.ViewControllers = new[] { vc };
				return;
			}

			//
			// Else, present a new nav VC
			//
			var nc = new UINavigationController (vc);

			var animate = true;

			if (rootVC.PresentedViewController != null) {
				rootVC.DismissViewController (false, null);
				animate = false;
			}

			presentedNav = nc;

			rootVC.PresentViewController (nc, animate, null);
		}
	}
}

