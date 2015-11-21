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

		public class ObjectElement
		{
			public string Title;
			public object Value;
		}

		public ObjectProperty[] Properties { get; private set; }

		public Type[] Hierarchy { get; private set; }

		public ObjectElement[] Elements { get; private set; }

		public string Title { get; private set; }

		public string ToStringValue { get; private set; }
		public string HashDisplayString { get; private set; }

		public bool IsList { get; private set; }

		public ObjectInspectorData ()
			: this (null)
		{
		}

		ObjectProperty GetObjectProperty (string name, Func<object, object> getValue)
		{
			object val = null;
			string valStr = "";
			try {
				val = getValue (target);

				if (val == null) {
					valStr = "null";
				}
				else {
					var l = val as System.Collections.IList;
					if (l != null) {
						valStr = string.Format ("#{0} {1}", l.Count, l.Count > 0 ? l[0].GetType().Name : "");
					}
					else {
						var d = val as IDictionary;
						if (d != null) {
							var typeStr = "";
							if (d.Count > 0) {
								var kv = d.Keys.Cast<object> ().First();
								var vv = d.Values.Cast<object> ().First();
								var kt = kv != null ? kv.GetType () : typeof(object);
								var vt = vv != null ? vv.GetType () : typeof(object);
								typeStr = kt.Name + ": " + vt.Name;
							}
							valStr = string.Format ("#{0} {1}", d.Count, typeStr);
						}
						else {
							valStr = val != null ? val.ToString () : "null";
						}
					}
				}
			} catch (Exception ex) {
				var iex = GetInnerException (ex);
				valStr = string.Format ("{0}: {1}", iex.GetType ().Name, iex.Message);
				Log (ex);
			}
			return new ObjectProperty {
				Name = name,
				Value = val,
				ValueString = valStr,
			};
		}

		ObjectProperty GetObjectPropertyFromProperty (PropertyInfo p)
		{
			return GetObjectProperty (p.Name, p.GetValue);
		}

		ObjectProperty GetObjectPropertyFromField (FieldInfo f)
		{
			return GetObjectProperty (f.Name, f.GetValue);
		}

		public ObjectInspectorData (object target)
		{
			this.target = target;
			this.targetType = target != null ? target.GetType () : typeof(object);

			IsList = (target is IList) || (target is IDictionary);

			if (IsList) {
				Properties = new ObjectProperty[0];
				Hierarchy = new Type[0];
			} else {

				var props = targetType.GetProperties ().
					Where (x =>
						x.GetIndexParameters ().Length == 0 &&
						x.GetMethod != null &&
						!x.GetMethod.IsStatic).
					Select (GetObjectPropertyFromProperty);
				var fields = targetType.GetFields ().
					Where (x =>
						!x.IsStatic).
					Select (GetObjectPropertyFromField);

				Properties = fields.Concat (props).OrderBy (x => x.Name).ToArray ();

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
			}

			var elementMax = 1000;

			if (this.target is IDictionary) {
				var res = new List<ObjectElement> ();
				IDictionaryEnumerator de = ((IDictionary)this.target).GetEnumerator ();
				while (res.Count < elementMax && de.MoveNext ()) {
					res.Add (new ObjectElement {
						Title = string.Format ("{0}: {1}", de.Key, de.Value),
						Value = de.Value,
					});
				}
				Elements = res.ToArray ();
			} else {

				var ie = this.target as IEnumerable;
				if (ie != null && !(this.target is string)) {
					try {
						Elements = ie.Cast<object> ().Take (elementMax).Select ((x, i) => new ObjectElement {
							Title = string.Format ("{0}: {1}", i, x),
							Value = x,
						}).ToArray ();					
					} catch (Exception ex) {
						Log (ex);
					}
				}
			}
			if (Elements == null) {
				Elements = new ObjectElement[0];
			}

			Title = this.targetType.Name;

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

