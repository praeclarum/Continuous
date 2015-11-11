using System;
using UIKit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveCode.Server
{
	public partial class Visualizer
	{
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

		UIViewController GetViewer (EvalRequest req, EvalResponse resp)
		{
			var vc = resp.Result as UIViewController;

			if (vc != null)
				return vc;


			var v = GetSpecialView (resp.Result);

			if (v != null) {
				vc = new UIViewController ();
				vc.View = v;
			}
			else {
				vc = new ObjectInspector (resp.Result);
			}
			return vc;
		}

		UINavigationController presentedNav = null;

		async Task ShowViewerAsync (UIViewController vc)
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

			var canBeInNav = CanBeInNav (vc);

			//
			// Try to just swap out the root VC if we've already presented
			//
			if (canBeInNav && presentedNav != null && rootVC.PresentedViewController == presentedNav) {
				presentedNav.ViewControllers = new[] { vc };
				return;
			}

			//
			// Else, present a new nav VC
			//
			var nc = canBeInNav ? new UINavigationController (vc) : null;

			var animate = true;

			if (rootVC.PresentedViewController != null) {
				await rootVC.DismissViewControllerAsync (false);
				animate = false;
			}

			presentedNav = nc;

			await rootVC.PresentViewControllerAsync (nc ?? vc, animate);
		}

		bool CanBeInNav (UIViewController vc)
		{
			return !(vc is UISplitViewController) && !(vc is UINavigationController);
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

