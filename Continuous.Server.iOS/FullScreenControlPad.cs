using System;

using CoreGraphics;
using Foundation;
using UIKit;

namespace Continuous.Server.iOS
{
	public class FullScreenControlPad : UIView
	{
		public event EventHandler Dismissed;

		static readonly CGSize defaultSize = new CGSize (44, 44);

		readonly UIButton dismissButton = UIButton.FromType (UIButtonType.RoundedRect);

		public FullScreenControlPad ()
			: base(new CGRect(new CGPoint (0, 0), defaultSize))
		{
			BackgroundColor = UIColor.FromWhiteAlpha (0.0f, 0.5f);
			ClipsToBounds = true;

			RestoreXY ();

			dismissButton.Frame = Bounds;
			dismissButton.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			dismissButton.TintColor = UIColor.White;
			dismissButton.SetTitle ("Dismiss " + Frame, UIControlState.Normal);
			dismissButton.TouchUpInside += (sender, e) => Dismissed?.Invoke (this, EventArgs.Empty);
			AddSubview (dismissButton);
		}

		static CGRect MoveRectIntoBounds (CGRect ifr)
		{
			var fr = ifr;

			var sb = UIApplication.SharedApplication.KeyWindow.Bounds;
			if (fr.X < 0) fr.X = 0;
			else if (fr.Right > sb.Width) fr.X = sb.Width - fr.Width;
			if (fr.Y < 0) fr.Y = 0;
			else if (fr.Bottom > sb.Height) fr.Y = sb.Height - fr.Height;

			return fr;
		}

		void RestoreXY ()
		{
			var defs = NSUserDefaults.StandardUserDefaults;
			var x = defs.FloatForKey ("Continuous.Server.FullScreenControlPad.X");
			var y = defs.FloatForKey ("Continuous.Server.FullScreenControlPad.Y");
			Frame = MoveRectIntoBounds (new CGRect (new CGPoint(x, y), defaultSize));
		}

		void SaveXY ()
		{
			var fr = Frame;
			var defs = NSUserDefaults.StandardUserDefaults;
			defs.SetFloat ((float)fr.X, "Continuous.Server.FullScreenControlPad.X");
			defs.SetFloat ((float)fr.Y, "Continuous.Server.FullScreenControlPad.Y");
		}
	}
}
