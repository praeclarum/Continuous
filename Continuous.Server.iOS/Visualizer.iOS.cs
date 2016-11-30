using System;
using UIKit;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using System.Linq;

namespace Continuous.Server
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
				presentedVC = null;
				rootVC.DismissViewController (false, null);
			}
		}

		partial void PlatformVisualize (EvalResult res)
		{
			var val = res.Result;
			var ty = val != null ? val.GetType () : typeof(object);

			Log ("{0} value = {1}", ty.FullName, val);

			ShowViewerAsync (GetViewer (res.Result, true)).ContinueWith (t => {
				if (t.IsFaulted) {
					Log ("ShowViewer ERROR {0}", t.Exception);
				}
			});
		}

		public UIViewController GetViewer (object value, bool createInspector)
		{
			var vc = value as UIViewController;
			if (vc != null)
				return vc;
			
			var sv = GetSpecialView (value);

			vc = sv as UIViewController;
			if (vc != null && vc.ParentViewController == null) {
				return vc;
			}

			var v = sv as UIView;
			if (v != null && v.Superview == null) {
				vc = new UIViewController ();
				vc.View = v;
				return vc;
			}

			if (createInspector) {
				vc = new ObjectInspector(value);
				return vc;
			}

			return null;
		}

		UIViewController presentedVC = null;

		async Task ShowViewerAsync (UIViewController vc)
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			if (window == null)
				return;
			var rootVC = window.RootViewController;
			if (rootVC == null)
				return;

			var pvc = vc;
			if (CanBeInNav(vc))
			{
				var nc = new UINavigationController(vc);
				nc.NavigationBarHidden = true;
				pvc = nc;
			}

			//
			// Try to just swap out the root VC if we've already presented
			//
			var needsPresent = false;
			if (presentedVC != null && rootVC.PresentedViewController == presentedVC)
			{
				//
				// Remove old stuff
				//
				var oldChildren = presentedVC.ChildViewControllers;
				foreach (var c in oldChildren)
				{
					c.RemoveFromParentViewController();
					c.View.RemoveFromSuperview();
				}
			}
			else
			{
				presentedVC = new UIViewController();
				needsPresent = true;
			}

			pvc.View.Frame = presentedVC.View.Bounds;
			pvc.View.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			presentedVC.View.AddSubview(pvc.View);
			presentedVC.AddChildViewController(pvc);

			if (needsPresent) {
				//
				// Else, present a new nav VC
				//
				var animate = true;

				if (rootVC.PresentedViewController != null)
				{
					await rootVC.DismissViewControllerAsync(false);
					animate = false;
				}

				await rootVC.PresentViewControllerAsync(presentedVC, animate);
			}
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
				{ typeof(UIImage).FullName, o => GetView ((UIImage)o) },
				{ typeof(CGImage).FullName, o => GetView (UIImage.FromImage ((CGImage)o)) },
				{ typeof(CoreImage.CIImage).FullName, o => GetView (UIImage.FromImage ((CoreImage.CIImage)o)) },
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

		UIView GetView (UIImage value)
		{
			return new UIImageView { Image = value, ContentMode = UIViewContentMode.ScaleAspectFit };
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

