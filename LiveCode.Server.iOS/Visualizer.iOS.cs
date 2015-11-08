using System;
using UIKit;
using System.Collections.Generic;

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
			var vc = resp.Result as UIViewController;

			if (vc != null)
				return vc;

			vc = new UIViewController ();

			var v = GetSpecialView (resp.Result);

			if (v != null) {
				vc.View = v;
			}
			else {
				var tv = new UITextView {
					Text = resp.Result.ToString (),
					Font = UIFont.FromDescriptor (UIFontDescriptor.PreferredHeadline, (nfloat)36.0),
					TextAlignment = UITextAlignment.Center,
				};
				vc.View = tv;
			}
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

		UIView GetSpecialView (object obj)
		{
			if (obj == null)
				return null;

			var v = FindVisualizer (obj.GetType ());
			if (v != null) {
				return v (obj);
			} else {
				return null;
			}
		}

		delegate UIView TypeVisualizer (object value);

		TypeVisualizer FindVisualizer (Type type)
		{
			if (type == null)
				return null;
			
			TypeVisualizer v;
			if (typeVisualizers.TryGetValue (type, out v)) {
				return v;
			}

			if (type == typeof(object))
				return null;

			return FindVisualizer (type.BaseType);
		}

		partial void PlatformInitialize ()
		{
			typeVisualizers = new Dictionary<Type, TypeVisualizer> {
				{ typeof(UIView), o => GetView ((UIView)o) },
				{ typeof(UITableViewCell), o => GetView ((UITableViewCell)o) },
				{ typeof(UIColor), o => GetView ((UIColor)o) },
			};
		}
		Dictionary<Type, TypeVisualizer> typeVisualizers = new Dictionary<Type, TypeVisualizer> ();

		UIView GetView (UIView value)
		{
			return value;
		}

		UIView GetView (UIColor value)
		{
			return new UIView { BackgroundColor = value, };
		}

		UIView GetView (UITableViewCell value)
		{
			var tableView = new UITableView ();
			tableView.DataSource = new SingleTableViewCellDataSource (value);
			return tableView;
		}

		class SingleTableViewCellDataSource : UITableViewDataSource
		{
			UITableViewCell cell;
			public SingleTableViewCellDataSource (UITableViewCell cell)
			{
				this.cell = cell;
			}
			public override nint NumberOfSections (UITableView tableView)
			{
				return 1;
			}
			public override nint RowsInSection (UITableView tableView, nint section)
			{
				return 1;
			}
			public override UITableViewCell GetCell (UITableView tableView, Foundation.NSIndexPath indexPath)
			{
				return cell;
			}
		}
	}
}

