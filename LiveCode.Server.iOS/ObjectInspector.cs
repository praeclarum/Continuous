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
		readonly object target;
		readonly Type targetType;

		readonly PropertyInfo[] properties;

		readonly Type[] hierarchy;

		readonly object[] elements;

		public ObjectInspector ()
			: this (new UISlider ())
		{
		}

		public ObjectInspector (object target)
			: this (target, target != null ? target.GetType () : typeof(object))
		{
		}

		public ObjectInspector (object target, Type targetType)
			: base (UITableViewStyle.Grouped)
		{
			this.target = target;
			this.targetType = targetType ?? typeof(object);

			properties = targetType.GetProperties ().
				Where (x => x.DeclaringType == this.targetType &&
					        x.GetIndexParameters().Length == 0 &&
					        x.GetMethod != null &&
					        !x.GetMethod.IsStatic).
				ToArray ();

			var h = new List<Type> ();
			h.Add (this.targetType);
			while (h [h.Count - 1] != typeof(object)) {
				var bt = h [h.Count - 1].BaseType;
				if (bt != null) {
					h.Add (bt);
				} else {
					break;
				}
			}
			h.Remove (this.targetType);
			h.Remove (typeof(object));
			h.Remove (typeof(ValueType));
			hierarchy = h.ToArray ();

			var ie = this.target as IEnumerable;
			if (ie != null && !(this.target is string)) {
				try {
					elements = ie.Cast<object> ().Take (100).ToArray ();					
				} catch (Exception ex) {
					Log (ex);
				}
			}
			if (elements == null) {
				elements = new object[0];
			}

			Title = this.targetType.FullName;
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
				return properties.Length;
			if (section == 2)
				return hierarchy.Length;
			if (section == 3)
				return elements.Length;
			return 0;
		}

		public override string TitleForHeader (UITableView tableView, nint section)
		{
			if (section == 0)
				return "";
			if (section == 1)
				return targetType.Name + " Properties";
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
					c.TextLabel.Text = "";
					try {
						c.TextLabel.Text = target.ToString ();
					} catch (Exception ex) {
						c.TextLabel.Text = ex.Message;
						Log (ex);
					}
					return c;
				} else {
					var c = tableView.DequeueReusableCell ("GH");
					if (c == null) {
						c = new UITableViewCell (UITableViewCellStyle.Default, "GH");
					}
					c.TextLabel.Text = "";
					try {
						c.TextLabel.Text = "#" + target.GetHashCode ();
					} catch (Exception ex) {
						Log (ex);
					}
					return c;
				}
			} else if (indexPath.Section == 2) {
				var c = tableView.DequeueReusableCell ("H");
				if (c == null) {
					c = new UITableViewCell (UITableViewCellStyle.Default, "H");
					c.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				}
				c.TextLabel.Text = "";
				try {
					c.TextLabel.TextColor = tableView.TintColor;
					c.TextLabel.Text = hierarchy[indexPath.Row].Name;
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
					c.TextLabel.Text = elements[indexPath.Row].ToString ();
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
				}
				try {
					c.DetailTextLabel.TextColor = tableView.TintColor;

					var prop = properties [indexPath.Row];
					c.TextLabel.Text = prop.Name;

					try {
						var v = prop.GetValue (target);
						c.DetailTextLabel.Text = v == null ? "null" : v.ToString ();

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

				var prop = properties [indexPath.Row];
				try {
					var v = prop.GetValue (target);
					var vc = new ObjectInspector (v);
					n.PushViewController (vc, true);
				} catch (Exception ex) {
					Log (ex);
				}

			} else if (indexPath.Section == 2) {

				var asTyp = hierarchy [indexPath.Row];
				var vc = new ObjectInspector (target, asTyp);

				n.PushViewController (vc, true);

			} else if (indexPath.Section == 3) {

				var e = elements [indexPath.Row];
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

