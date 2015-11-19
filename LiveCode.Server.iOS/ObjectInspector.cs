using System;
using UIKit;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace LiveCode.Server
{
	public class ObjectInspector : UITableViewController
	{
		readonly ObjectInspectorData data;

		public ObjectInspector ()
			: this (new UISlider ())
		{
		}

		public ObjectInspector (object target)
			: base (UITableViewStyle.Grouped)
		{
			data = new ObjectInspectorData (target);

			Title = data.Title;
		}

		public override nint NumberOfSections (UITableView tableView)
		{
			return 4;
		}

		public override nint RowsInSection (UITableView tableView, nint section)
		{
			if (section == 0)
				return 2;
			if (section == 1)
				return data.Properties.Length;
			if (section == 2)
				return data.Hierarchy.Length;
			if (section == 3)
				return data.Elements.Length;
			return 0;
		}

		public override string TitleForHeader (UITableView tableView, nint section)
		{
			if (section == 0)
				return "";
			if (section == 1)
				return "Properties";
			if (section == 2)
				return "Hierarchy";
			if (section == 3)
				return "IEnumerable Elements";
			return "";
		}

		public override nfloat GetHeightForRow (UITableView tableView, Foundation.NSIndexPath indexPath)
		{
			if (indexPath.Section == 0 && indexPath.Row == 0)
				return 88.0f;
			return 44.0f;
		}

		public override UITableViewCell GetCell (UITableView tableView, Foundation.NSIndexPath indexPath)
		{
			if (indexPath.Section == 0) {
				if (indexPath.Row == 0) {
					
					var c = tableView.DequeueReusableCell ("TS");
					if (c == null) {
						c = new UITableViewCell (UITableViewCellStyle.Default, "TS");
						c.TextLabel.Lines = 3;
						c.TextLabel.AdjustsFontSizeToFitWidth = true;
						c.TextLabel.Font = UIFont.BoldSystemFontOfSize (22.0f);
					}
					c.TextLabel.Text = data.ToStringValue;
					return c;
				} else {
					var c = tableView.DequeueReusableCell ("GH");
					if (c == null) {
						c = new UITableViewCell (UITableViewCellStyle.Default, "GH");
					}
					c.TextLabel.Text = data.HashDisplayString;
					return c;
				}
			} else if (indexPath.Section == 2) {
				var c = tableView.DequeueReusableCell ("H");
				if (c == null) {
					c = new UITableViewCell (UITableViewCellStyle.Default, "H");
				}
				c.TextLabel.Text = "";
				try {
					c.TextLabel.TextColor = tableView.TintColor;
					c.TextLabel.Text = data.Hierarchy[indexPath.Row].Name;
				} catch (Exception ex) {
					Log (ex);
				}
				return c;
			
			} else if (indexPath.Section == 3) {
				var c = tableView.DequeueReusableCell ("E");
				if (c == null) {
					c = new UITableViewCell (UITableViewCellStyle.Default, "E");
					c.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				}
				c.TextLabel.Text = "";
				try {
					c.TextLabel.TextColor = tableView.TintColor;
					c.TextLabel.Text = data.Elements[indexPath.Row].ToString ();
				} catch (Exception ex) {
					Log (ex);
				}
				return c;
			} else {
				var c = tableView.DequeueReusableCell ("P");
				if (c == null) {
					c = new UITableViewCell (UITableViewCellStyle.Subtitle, "P");
					c.TextLabel.Font = UIFont.FromDescriptor (UIFontDescriptor.PreferredCaption1, 12.0f);
					c.DetailTextLabel.Font = UIFont.FromDescriptor (UIFontDescriptor.PreferredCaption1, 16.0f);
					c.DetailTextLabel.MinimumFontSize = 10.0f;
					c.DetailTextLabel.AdjustsFontSizeToFitWidth = true;
				}
				try {
					c.DetailTextLabel.TextColor = tableView.TintColor;

					var prop = data.Properties [indexPath.Row];
					c.TextLabel.Text = prop.Name;

					try {
						var v = prop.Value;
						c.DetailTextLabel.Text = prop.ValueString;

						if (v != null && !IsPrimitive (v.GetType ())) {
							c.Accessory = UITableViewCellAccessory.DisclosureIndicator;
						} else {
							c.Accessory = UITableViewCellAccessory.None;
						}

					} catch (Exception ex) {
						Log (ex);
						var i = ex;
						while (i.InnerException != null) {
							i = i.InnerException;
						}
						c.DetailTextLabel.Text = i.Message;
						c.DetailTextLabel.TextColor = UIColor.Red;
						c.Accessory = UITableViewCellAccessory.None;
					}
				} catch (Exception ex) {
					Log (ex);
				}
				return c;
			}
		}

		public override void RowSelected (UITableView tableView, Foundation.NSIndexPath indexPath)
		{
			var n = NavigationController;
			if (n == null)
				return;
			
			if (indexPath.Section == 1) {

				var prop = data.Properties [indexPath.Row];
				try {
					var v = prop.Value;
					var vc = new ObjectInspector (v);
					n.PushViewController (vc, true);
				} catch (Exception ex) {
					Log (ex);
				}

			} else if (indexPath.Section == 3) {

				var e = data.Elements [indexPath.Row];
				var vc = new ObjectInspector (e);

				n.PushViewController (vc, true);
			}
		}

		bool IsPrimitive (Type type)
		{
			return type.IsPrimitive || type.IsEnum;
		}

		void Log (Exception ex)
		{
			Console.WriteLine (ex);
		}
	}
}

