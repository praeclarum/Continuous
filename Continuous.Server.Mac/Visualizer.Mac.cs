using System;
using AppKit;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using System.Linq;

namespace Continuous.Server
{
	public partial class Visualizer
	{
		partial void PlatformStopVisualizing()
		{
		}

		partial void PlatformVisualize(EvalResult res)
		{
		}

		public NSViewController GetViewer(object value, bool createInspector)
		{
			var vc = value as NSViewController;
			if (vc != null)
				return vc;

			var sv = GetSpecialView(value);

			vc = sv as NSViewController;
			if (vc != null && vc.ParentViewController == null)
			{
				return vc;
			}

			var v = sv as NSView;
			if (v != null && v.Superview == null)
			{
				vc = new NSViewController();
				vc.View = v;
				return vc;
			}

			return null;
		}

		async Task ShowViewerAsync(NSViewController vc)
		{
		}

		object GetSpecialView(object obj)
		{
			if (obj == null)
				return null;

			var v = FindVisualizer(obj.GetType());
			if (v != null)
			{
				return v(obj);
			}
			else
			{
				return null;
			}
		}

		delegate object TypeVisualizer(object value);

		TypeVisualizer FindVisualizer(Type type)
		{
			if (type == null)
				return null;

			TypeVisualizer v;
			if (typeVisualizers.TryGetValue(type.FullName, out v))
			{
				return v;
			}

			if (type == typeof(object))
				return null;

			return FindVisualizer(type.BaseType);
		}

		partial void PlatformInitialize()
		{
			typeVisualizers = new Dictionary<string, TypeVisualizer> {
				{ typeof(NSView).FullName, o => GetView ((NSView)o) },
				{ typeof(NSImage).FullName, o => GetView ((NSImage)o) },
				{ typeof(CGImage).FullName, o => GetView (new NSImage ((CGImage)o, new CGSize(((CGImage)o).Width, ((CGImage)o).Height))) },
				{ typeof(string).FullName, o => GetView ((string)o) },
				{ "Xamarin.Forms.Page", GetFormsPage },
				{ "Xamarin.Forms.View", GetFormsView },
			};
		}
		Dictionary<string, TypeVisualizer> typeVisualizers = new Dictionary<string, TypeVisualizer>();

		public virtual NSView GetView(NSView value)
		{
			return value;
		}

		public virtual NSView GetView(NSImage value)
		{
			return new NSImageView { Image = value };
		}

		public virtual NSView GetView(string value)
		{
			return new NSTextView { Value = value };
		}

		public virtual System.Reflection.Assembly GetXamarinCoreAsm()
		{
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			return asms.First(x => x.GetName().Name == "Xamarin.Forms.Core");
		}

		public virtual System.Reflection.Assembly GetXamarinPlatformAsm()
		{
			var asms = AppDomain.CurrentDomain.GetAssemblies();
			return asms.First(x => x.GetName().Name == "Xamarin.Forms.Platform.iOS");
		}

		public virtual NSViewController GetFormsPage(object pageObj)
		{
			var platasm = GetXamarinPlatformAsm();

			// Create the VC
			var pagex = platasm.GetType("Xamarin.Forms.PageExtensions");
			var cvc = pagex.GetMethod("CreateViewController");
			var vc = (NSViewController)cvc.Invoke(null, new[] { pageObj });

			return vc;
		}

		public virtual NSViewController GetFormsView(object viewObj)
		{
			var xamasm = GetXamarinCoreAsm();

			// Create a ContentPage to hold this view
			var page = xamasm.GetType("Xamarin.Forms.ContentPage");
			var pageObj = Activator.CreateInstance(page, viewObj);

			return GetFormsPage(pageObj);
		}
	}
}

