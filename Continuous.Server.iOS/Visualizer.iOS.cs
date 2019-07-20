﻿using System;
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
				vc = new UINavigationController (new ObjectInspector(value));
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
					c.ViewWillDisappear(false);
					c.RemoveFromParentViewController();
					c.View.RemoveFromSuperview();
					c.ViewDidDisappear(false);
				}
			}
			else
			{
				presentedVC = new UIViewController { ModalPresentationStyle = UIModalPresentationStyle.FullScreen };
				needsPresent = true;
			}

			pvc.View.Frame = presentedVC.View.Bounds;
			pvc.View.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;

			pvc.ViewWillAppear(false);
			presentedVC.View.AddSubview(pvc.View);
			presentedVC.AddChildViewController(pvc);
			pvc.ViewDidAppear(false);

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
				if (v != null)
					return v;
			}

			if (type == typeof(object))
				return null;

			return FindVisualizer (type.BaseType);
		}

		partial void PlatformInitialize ()
		{
			typeVisualizers = new Dictionary<string, TypeVisualizer> {
				{ typeof(UIApplicationDelegate).FullName, o => GetView ((UIApplicationDelegate)o) },
				{ typeof(UIWindow).FullName, o => GetView ((UIWindow)o) },
				{ typeof(UIView).FullName, o => GetView ((UIView)o) },
				{ typeof(UITableViewCell).FullName, o => GetView ((UITableViewCell)o) },
				{ typeof(UICollectionViewCell).FullName, o => GetView ((UICollectionViewCell)o) },
				{ typeof(UIColor).FullName, o => GetView ((UIColor)o) },
				{ typeof(UIImage).FullName, o => GetView ((UIImage)o) },
				{ typeof(CGImage).FullName, o => GetView (UIImage.FromImage ((CGImage)o)) },
				{ typeof(CoreImage.CIImage).FullName, o => GetView (UIImage.FromImage ((CoreImage.CIImage)o)) },
				{ typeof(string).FullName, o => GetView ((string)o) },
				{ "Xamarin.Forms.Page", GetFormsPage },
				{ "Xamarin.Forms.View", GetFormsView },
			};
		}
		Dictionary<string, TypeVisualizer> typeVisualizers = new Dictionary<string, TypeVisualizer> ();

		public virtual object GetView (UIApplicationDelegate value)
		{
			//
			// Is there a window? If so, show that
			//
			if (value.Window != null)
			{
				return GetView (value.Window);
			}

			//
			// What if we fake run the life cycle?
			//
			var launchOptions = new Foundation.NSDictionary ();
			try
			{
				value.WillFinishLaunching (UIApplication.SharedApplication, launchOptions);
			}
			catch (Exception)
			{
			}
			try
			{
				value.FinishedLaunching (UIApplication.SharedApplication, launchOptions);
			}
			catch (Exception)
			{
			}
			if (value.Window != null)
			{
				return GetView (value.Window);
			}

			//
			// Just show the object inspector
			//
			return null;
		}

		public virtual object GetView (UIWindow value)
		{
			if (value.IsKeyWindow)
			{
				value.ResignKeyWindow ();
			}
			var root = value.RootViewController;
			if (root != null)
			{
				// Replace the root so we can display it again
				var tempvc = new UIViewController ();
				value.RootViewController = tempvc;
				return root;
			}
			return value;
		}

		public virtual UIView GetView (UIView value)
		{
			return value;
		}

		public virtual UIView GetView (UIColor value)
		{
			return new UIView { BackgroundColor = value, };
		}

		public virtual UIView GetView (UIImage value)
		{
			return new UIImageView { Image = value, ContentMode = UIViewContentMode.ScaleAspectFit };
		}

		public virtual UIView GetView (string value)
		{
			return new UITextView { Text = value };
		}

		public virtual UIView GetView (UITableViewCell value)
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

		public virtual UIView GetView (UICollectionViewCell value)
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

		public virtual System.Reflection.Assembly GetXamarinCoreAsm()
		{
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			return asms.First(x => x.GetName().Name == "Xamarin.Forms.Core");
		}

		public virtual System.Reflection.Assembly GetXamarinPlatformAsm ()
		{
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			return asms.First(x => x.GetName().Name == "Xamarin.Forms.Platform.iOS");
		}

		public virtual UIViewController GetFormsPage (object pageObj)
		{
			var platasm = GetXamarinPlatformAsm ();

			// Create the VC
			var pagex = platasm.GetType ("Xamarin.Forms.PageExtensions");
			var cvc = pagex.GetMethod ("CreateViewController");
			var vc = (UIViewController)cvc.Invoke (null, new[]{ pageObj });

			return vc;
		}

		public virtual UIViewController GetFormsView (object viewObj)
		{
			var xamasm = GetXamarinCoreAsm();

			// Create a ContentPage to hold this view
			var page = xamasm.GetType("Xamarin.Forms.ContentPage");
			var pageObj = Activator.CreateInstance(page, viewObj);

			return GetFormsPage (pageObj);
		}
	}
}

