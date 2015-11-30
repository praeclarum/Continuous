using System;
using UIKit;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using System.Linq;

namespace LiveCode.Server
{
	public partial class Visualizer
	{
		partial void PlatformStopVisualizing ()
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			if (window == null)
				return;
			var rootVC = window.RootViewController;
			if (rootVC == null)
				return;

			if (rootVC.PresentedViewController != null) {
				rootVC.DismissViewController (false, null);
			}
		}

		partial void PlatformVisualize (EvalResult res)
		{
			var val = res.Result;
			var ty = val != null ? val.GetType () : typeof(object);

			Log ("{0} value = {1}", ty.FullName, val);

			ShowViewerAsync (GetViewer (res)).ContinueWith (t => {
				if (t.IsFaulted) {
					Log ("ShowViewer ERROR {0}", t.Exception);
				}
			});
		}

		Tuple<UIViewController, bool> GetViewer (EvalResult resp)
		{
			var vc = resp.Result as UIViewController;
			if (vc != null)
				return Tuple.Create (vc, true);
			
			var sv = GetSpecialView (resp.Result);

			vc = sv as UIViewController;
			if (vc != null)
				return Tuple.Create (vc, false);

			var v = sv as UIView;
			if (v != null) {
				vc = new UIViewController ();
				vc.View = v;
				return Tuple.Create (vc, false);
			}

			vc = new ObjectInspector (resp.Result);
			return Tuple.Create (vc, true);
		}

		UINavigationController presentedNav = null;

		async Task ShowViewerAsync (Tuple<UIViewController, bool> vcNC)
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			if (window == null)
				return;
			var rootVC = window.RootViewController;
			if (rootVC == null)
				return;

			var vc = vcNC.Item1;
			var shouldBeInNav = CanBeInNav (vc) && vcNC.Item2;

			if (shouldBeInNav) {
				var doneButton = new UIBarButtonItem (
					                UIBarButtonSystemItem.Done,
					                (_, __) => rootVC.DismissViewController (true, null));
				vc.NavigationItem.RightBarButtonItems =
					new[]{ doneButton }.
					Concat (vc.NavigationItem.RightBarButtonItems ?? new UIBarButtonItem[0]).
					ToArray ();
			}

			//
			// Try to just swap out the root VC if we've already presented
			//
			if (shouldBeInNav && presentedNav != null && rootVC.PresentedViewController == presentedNav) {
				presentedNav.ViewControllers = new[] { vc };
				return;
			}

			//
			// Else, present a new nav VC
			//
			var nc = shouldBeInNav ? new UINavigationController (vc) : null;

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

		object GetSpecialView (object obj)
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

		delegate object TypeVisualizer (object value);

		TypeVisualizer FindVisualizer (Type type)
		{
			if (type == null)
				return null;
			
			TypeVisualizer v;
			if (typeVisualizers.TryGetValue (type.FullName, out v)) {
				return v;
			}

			if (type == typeof(object))
				return null;

			return FindVisualizer (type.BaseType);
		}

		partial void PlatformInitialize ()
		{
			typeVisualizers = new Dictionary<string, TypeVisualizer> {
				{ typeof(UIView).FullName, o => GetView ((UIView)o) },
				{ typeof(UITableViewCell).FullName, o => GetView ((UITableViewCell)o) },
				{ typeof(UICollectionViewCell).FullName, o => GetView ((UICollectionViewCell)o) },
				{ typeof(UIColor).FullName, o => GetView ((UIColor)o) },
				{ "Xamarin.Forms.Page", GetFormsPage },
			};
		}
		Dictionary<string, TypeVisualizer> typeVisualizers = new Dictionary<string, TypeVisualizer> ();

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

		UIView GetView (UICollectionViewCell value)
		{
			var layout = new UICollectionViewFlowLayout ();
			var bounds = UIScreen.MainScreen.Bounds;
			var collectionView = new UICollectionView (
				                     bounds,
				                     layout);
			layout.ItemSize = new CGSize (bounds.Width / 3, bounds.Width / 4);
			collectionView.RegisterClassForCell (typeof (HostCell), "C");
			collectionView.DataSource = new SingleCollectionViewCellDataSource (value);
			return collectionView;
		}

		class SingleCollectionViewCellDataSource : UICollectionViewDataSource
		{
			UICollectionViewCell cell;
			public SingleCollectionViewCellDataSource (UICollectionViewCell cell)
			{
				this.cell = cell;
			}
			public override nint NumberOfSections (UICollectionView collectionView)
			{
				return 1;
			}
			public override nint GetItemsCount (UICollectionView collectionView, nint section)
			{
				return 1;
			}
			public override UICollectionViewCell GetCell (UICollectionView collectionView, Foundation.NSIndexPath indexPath)
			{
				var c = collectionView.DequeueReusableCell ("C", indexPath) as HostCell;
				if (c.VizView != cell) {
					c.VizView = cell;
					cell.Frame = c.ContentView.Bounds;
					cell.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
					c.ContentView.Add (cell);
				}
				return c;
			}
		}

		class HostCell : UICollectionViewCell
		{
			public UIView VizView;
			public HostCell ()
			{
			}
			public HostCell (IntPtr h) : base (h)
			{
			}
		}

		UIViewController GetFormsPage (object pageObj)
		{
			var asms = AppDomain.CurrentDomain.GetAssemblies ();
			var xamasm = asms.First (x => x.GetName ().Name == "Xamarin.Forms.Core");
			var platasm = asms.First (x => x.GetName ().Name == "Xamarin.Forms.Platform.iOS");

			// Wrap it in a NavigationPage cause I think it's needed?
//			var navpage = xamasm.GetType ("Xamarin.Forms.NavigationPage");
//			var navPageObj = Activator.CreateInstance (navpage, new object[]{ pageObj });

			// Create the VC
			var pagex = platasm.GetType ("Xamarin.Forms.PageExtensions");
			var cvc = pagex.GetMethod ("CreateViewController");

//			var nc = (UIViewController)cvc.Invoke (null, new[]{ navPageObj });
			var vc = (UIViewController)cvc.Invoke (null, new[]{ pageObj });

			return vc;
		}
	}
}

