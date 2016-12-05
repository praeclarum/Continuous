using System;
using CoreGraphics;
using UIKit;

namespace Continuous.Server.iOS
{
	public class FullScreenControlPad : UIView
	{
		public event EventHandler Dismissed;

		readonly UIButton dismissButton = UIButton.FromType (UIButtonType.RoundedRect);

		public FullScreenControlPad ()
			: base(new CGRect(0, 0, 44, 44))
		{
			BackgroundColor = UIColor.FromWhiteAlpha (0.0f, 0.5f);
			ClipsToBounds = true;

			dismissButton.Frame = Bounds;
			dismissButton.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			dismissButton.TintColor = UIColor.White;
			dismissButton.SetTitle ("Hello", UIControlState.Normal);
			dismissButton.TouchUpInside += (sender, e) => Dismissed?.Invoke (this, EventArgs.Empty);
			AddSubview (dismissButton);
		}
	}
}
