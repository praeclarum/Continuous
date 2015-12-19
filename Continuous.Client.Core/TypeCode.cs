using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace Continuous.Client
{
	public class LinkedCode
	{
		public readonly string Declarations;
		public readonly string ValueExpression;
		public readonly TypeCode[] Types;
		public readonly string CacheKey;
		public LinkedCode (string declarations, string valueExpression, TypeCode[] types, TypeCode mainType)
		{
			Declarations = declarations??"";
			ValueExpression = valueExpression??"";
			Types = types;

			CacheKey = mainType.UsingsAndCode + string.Join ("", types.Select (x => x.UsingsAndCode));
		}
	}

	public class TypeCode
	{
		// Only partial namespace support because I can't figureout how to
		// get good TypeDeclarations from SimpleTypes.

		public string Name = "";
		public TypeCode[] Dependencies = new TypeCode[0];
		public string[] Usings = new string[0];
		public string Code = "";
		public bool CodeChanged = false;
		public DateTime CodeChangedTime = DateTime.MinValue;
		public string FullNamespace = "";
		public WatchVariable[] Watches = new WatchVariable[0];

		public string Key {
			get { return Name; }
		}

		public override string ToString ()
		{
			return Name;
		}

		public bool HasCode { get { return !string.IsNullOrWhiteSpace (Code); } }
		public bool HasNamespace { get { return !string.IsNullOrWhiteSpace (FullNamespace); } }

		static readonly Dictionary<string, TypeCode> infos = new Dictionary<string, TypeCode> ();

		public static IEnumerable<TypeCode> All {
			get { return infos.Values; }
		}

		public static void Clear ()
		{
			infos.Clear ();
		}

		public static void ClearEdits ()
		{
			foreach (var t in infos.Values) {
				t.CodeChanged = false;
			}
		}

		public static TypeCode Get (string name)
		{			
			var key = name;
			TypeCode ci;
			if (infos.TryGetValue (key, out ci)) {
				return ci;
			}

			ci = new TypeCode {
				Name = name,
			};
			infos [key] = ci;
			return ci;
		}

		public static TypeCode Set (DocumentRef doc, TypeDeclaration typedecl, CSharpAstResolver resolver)
		{
			var ns = typedecl.Parent as NamespaceDeclaration;
			var nsName = ns == null ? "" : ns.FullName;
			var name = typedecl.Name;

			var usings =
				resolver.RootNode.Descendants.
				OfType<UsingDeclaration> ().
				Select (x => x.ToString ().Trim ()).
				ToList ();

			//
			// Find dependencies
			//
			var deps = new List<String> ();
			foreach (var d in typedecl.Descendants.OfType<SimpleType> ()) {
				deps.Add (d.Identifier);
			}

			//
			// Find watches
			//
			var watches = new List<WatchVariable> ();
			foreach (var d in typedecl.Descendants.OfType<VariableInitializer> ()) {
				var endLoc = d.EndLocation;
				var p = d.Parent;
				if (p == null)
					continue;
				var nc = p.GetNextSibling (x => x is Comment && x.StartLocation.Line == endLoc.Line);
				if (nc == null || !nc.ToString ().StartsWith ("//="))
					continue;
				watches.Add (new WatchVariable {
					Id = Guid.NewGuid (),
					Expression = d.Name,
					FilePath = doc.FullPath,
					FileLine = nc.StartLocation.Line,
					FileColumn = nc.StartLocation.Column,
				});
			}
			foreach (var d in typedecl.Descendants.OfType<AssignmentExpression> ()) {
				var endLoc = d.EndLocation;
				var p = d.Parent;
				if (p == null)
					continue;
				var nc = p.GetNextSibling (x => x is Comment && x.StartLocation.Line == endLoc.Line);
				if (nc == null || !nc.ToString ().StartsWith ("//="))
					continue;
				watches.Add (new WatchVariable {
					Id = Guid.NewGuid (),
					Expression = d.Left.ToString (),
					FilePath = doc.FullPath,
					FileLine = nc.StartLocation.Line,
					FileColumn = nc.StartLocation.Column,
				});
			}

			var code = typedecl.ToString ();

			return Set (name, usings, code, deps, nsName, watches);
		}
		public static TypeCode Set (string name, IEnumerable<string> usings, string code, IEnumerable<string> deps, string fullNamespace = "", IEnumerable<WatchVariable> watches = null)
		{
			var tc = Get (name);

			tc.Usings = usings.ToArray ();
			tc.Dependencies = deps.Distinct ().Select (Get).ToArray ();
			tc.FullNamespace = fullNamespace;
			tc.Watches = watches != null ? watches.ToArray () : new WatchVariable[0];

			var safeCode = code ?? "";

			if (!string.IsNullOrEmpty (safeCode)) {
				if (string.IsNullOrWhiteSpace (tc.Code)) {
					tc.Code = safeCode;
					tc.CodeChanged = false;
				} else {
					if (tc.Code != safeCode) {
						tc.Code = safeCode;
						tc.CodeChanged = true;
						tc.CodeChangedTime = DateTime.UtcNow;
					}
				}
			}

			return tc;
		}

		void GetDependencies (List<TypeCode> code)
		{
			if (code.Contains (this))
				return;
			code.Add (this);
			foreach (var d in Dependencies) {
				d.GetDependencies (code);
			}
			// Move us to the back
			code.Remove (this);
			code.Add (this);
		}

		public List<TypeCode> AllDependencies {
			get {
				var codes = new List<TypeCode> ();
				GetDependencies (codes);
				return codes;
			}
		}

		public string UsingsAndCode {
			get {
				return string.Join (Environment.NewLine, Usings) + Environment.NewLine + Code;
			}
		}

		public LinkedCode GetLinkedCode ()
		{
			var allDeps = AllDependencies.Where (x => x.HasCode).ToList ();

			var changedDeps = allDeps.Where (x => x.CodeChanged || x == this).ToList ();
			var notChangedDeps = allDeps.Where (x => !x.CodeChanged && x != this).ToList ();

			var changedDepsChanged = true;
			while (changedDepsChanged) {
				changedDepsChanged = false;
				foreach (var nc in notChangedDeps) {
					var ncDeps = nc.AllDependencies;
					var depChanged = ncDeps.Any (changedDeps.Contains);
					if (depChanged) {
						changedDeps.Add (nc);
						notChangedDeps.Remove (nc);
						changedDepsChanged = true;
						break;
					}
				}
			}
			var codes = changedDeps;

			var usings = codes.SelectMany (x => x.Usings).Distinct ().ToList ();

			var suffix = DateTime.UtcNow.Ticks.ToString ();

			var renames =
				codes.
				Select (x => Tuple.Create (
					new System.Text.RegularExpressions.Regex ("\\b" + x.Name + "\\b"),
					x.Name + suffix)).
				ToList ();

			Func<string, string> rename = c => {
				var rc = c;
				foreach (var r in renames) {
					rc = r.Item1.Replace (rc, r.Item2);
				}
				return rc;
			};

			return new LinkedCode (
				valueExpression:
					"new " +
					(HasNamespace ? FullNamespace + "." : "") +
					Name + suffix + "()",
				declarations:
					string.Join (Environment.NewLine, codes.Select (x => {
						var us = string.Join (Environment.NewLine, usings);
						var renamedCode = rename (x.Code);
						if (x.HasNamespace) {
							return "namespace " + x.FullNamespace + "{" + us + "\n" + renamedCode + "}";
						}
						else {
							return us + "\n" + renamedCode;
						}
					})),
				types: codes.ToArray (),
				mainType: this);
		}
	}
}

