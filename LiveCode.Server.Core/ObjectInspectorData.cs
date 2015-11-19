using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace LiveCode.Server
{
	public class ObjectInspectorData
	{
		readonly object target;
		readonly Type targetType;

		public object Target { get { return target; } }

		public class ObjectProperty
		{
			public string Name;
			public object Value;
			public string ValueString;
		}

		public ObjectProperty[] Properties { get; private set; }

		public Type[] Hierarchy { get; private set; }

		public object[] Elements { get; private set; }

		public string Title { get; private set; }

		public string ToStringValue { get; private set; }
		public string HashDisplayString { get; private set; }

		public ObjectInspectorData ()
			: this (null)
		{
		}

		public ObjectInspectorData (object target)
		{
			this.target = target;
			this.targetType = target != null ? target.GetType () : typeof(object);

			Properties = targetType.GetProperties ().
				Where (x =>
					x.GetIndexParameters ().Length == 0 &&
					x.GetMethod != null &&
					!x.GetMethod.IsStatic).
				OrderBy (x => x.Name).
				Select (x => {
					object val = null;
					string valStr = "";
					try {
						val = x.GetValue (target);
						valStr = val != null ? val.ToString () : "null";
					} catch (Exception ex) {
						var iex = GetInnerException (ex);
						valStr = string.Format ("{0}: {1}", iex.GetType ().Name, iex.Message);
						Log (ex);
					}
					return new ObjectProperty {
						Name = x.Name,
						Value = val,
						ValueString = valStr,
					};
				}).					
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
			Hierarchy = h.ToArray ();

			var ie = this.target as IEnumerable;
			if (ie != null && !(this.target is string)) {
				try {
					Elements = ie.Cast<object> ().Take (100).ToArray ();					
				} catch (Exception ex) {
					Log (ex);
				}
			}
			if (Elements == null) {
				Elements = new object[0];
			}

			Title = this.targetType.FullName;

			if (target == null) {
				ToStringValue = "null";
				HashDisplayString = "#0";
			}
			else {
				try {
					ToStringValue = target.ToString ();
				} catch (Exception ex) {
					ToStringValue = ex.ToString ();
					Log (ex);
				}
				try {
					HashDisplayString = "#" + target.GetHashCode ();
				} catch (Exception ex) {
					HashDisplayString = ex.ToString ();
					Log (ex);
				}
			}
		}

		Exception GetInnerException (Exception ex)
		{
			if (ex.InnerException == null)
				return ex;
			return GetInnerException (ex.InnerException);
		}

		void Log (Exception ex)
		{
			Console.WriteLine (ex);
		}
	}
}

