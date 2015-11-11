using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Gtk;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory;
using System.Collections.Generic;

namespace LiveCode.Client.XamarinStudio
{
	public class TypeCode
	{
		public string Name = "";
		public TypeCode[] Dependencies = new TypeCode[0];
		public string[] Usings = new string[0];
		public string Code = "";
		public bool NewCode = false;

		public string Key {
			get { return Name; }
		}

		public bool HasCode { get { return !string.IsNullOrWhiteSpace (Code); } }

		static readonly Dictionary<string, TypeCode> infos = new Dictionary<string, TypeCode> ();

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

		public static TypeCode Set (TypeDeclaration typedecl, CSharpAstResolver resolver)
		{
			var ns = typedecl.Parent as NamespaceDeclaration;
			var nsName = ns == null ? "" : ns.FullName;
			var name = typedecl.Name;

			var tc = Get (name);

			var usings =
				resolver.RootNode.Descendants.
				OfType<UsingDeclaration> ().
				Select (x => x.ToString ().Trim ()).
				ToList ();
			
			if (!string.IsNullOrWhiteSpace (nsName)) {
				var nsUsing = "using " + nsName + ";";
				usings.Add (nsUsing);
			}

			tc.Usings = usings.ToArray ();
			var code = typedecl.ToString ();
			if (tc.Code.Length > 0 && tc.Code != code) {
				tc.NewCode = true;
			}
			tc.Code = code;

			var deps = new List<String> ();
			foreach (var d in typedecl.Descendants.OfType<SimpleType> ()) {
				deps.Add (d.Identifier);
			}
			tc.Dependencies = deps.Distinct ().Select (Get).ToArray ();

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

		public LinkedCode GetLinkedCode ()
		{
			NewCode = true; // Force ourselves to link

			var codes = AllDependencies.Where (x => x.NewCode).ToList ();

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

			return new LinkedCode {
				ValueExpression = "new " + Name + suffix + "()",
				Declarations =
					usings.Concat (
						codes.
						Select (x => rename (x.Code))).
					ToArray (),
			};
		}
	}

	public class LinkedCode
	{
		public string[] Declarations;
		public string ValueExpression;
	}

	public enum Commands
	{
		VisualizeSelection,
		VisualizeClass,
	}

	public class LiveCodeCommandHandler : CommandHandler
	{
		HttpClient conn = null;
		protected void Connect ()
		{
			conn = new HttpClient (new Uri ("http://127.0.0.1:" + Http.DefaultPort));
		}

		protected void Alert (string format, params object[] args)
		{
			Console.WriteLine (format, args);
			var parentWindow = IdeApp.Workbench.RootWindow;
			var dialog = new MessageDialog(parentWindow, DialogFlags.DestroyWithParent,
				MessageType.Info, ButtonsType.Ok,
				format, args);
			dialog.Run ();
			dialog.Destroy ();
		}

		protected async Task<bool> EvalAsync (string code, bool showError)
		{
			var r = await conn.VisualizeAsync (code);
			var err = r.HasErrors;
			if (err) {
				var message = string.Join ("\n", r.Messages.Select (m => m.MessageType + ": " + m.Text));
				if (showError) {
					Alert ("{0}", message);
				}
			}
			return !err;
		}
	}

	public class VisualizeSelectionHandler : LiveCodeCommandHandler
	{		
		protected override async void Run ()
		{
			base.Run ();

			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				Connect ();
				var code = doc.Editor.SelectedText;

				try {
					await EvalAsync (code, showError: true);
				} catch (Exception ex) {
					Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
				}
			}
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var doc = IdeApp.Workbench.ActiveDocument;

			info.Enabled = doc != null && doc.Editor != null && !string.IsNullOrWhiteSpace (doc.Editor.SelectedText);
		}
	}

	public class VisualizeClassHandler : LiveCodeCommandHandler
	{
		string monitorTypeName = "";
//		string monitorNamespace = "";

		protected override async void Run ()
		{
			base.Run ();

			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc == null)
				return;

			var typedecl = await FindTypeAtCursor ();

			if (typedecl == null) {				
				Alert ("Could not find a type at the cursor.");
				return;
			}
			
			StartMonitoring ();

			var typeName = typedecl.Name;
			var ns = typedecl.Parent as NamespaceDeclaration;

			var nsName = ns == null ? "" : ns.FullName;

			Console.WriteLine ("MONITOR {0} --- {1}", nsName, typeName);

			monitorTypeName = typeName;
//			monitorNamespace = nsName;

			await VisualizeTypeAsync (showError: true);
		}

		async Task VisualizeTypeAsync (bool showError)
		{
			if (string.IsNullOrWhiteSpace (monitorTypeName))
				return;
			
			var doc = IdeApp.Workbench.ActiveDocument;
			var resolver = await doc.GetSharedResolver ();
			var typeDecls =
				resolver.RootNode.Descendants.
				OfType<TypeDeclaration> ().
				ToList ();

			var monitorTC = TypeCode.Get (monitorTypeName);

			var typeTCs = new List<TypeCode> ();
			foreach (var td in typeDecls) {
				typeTCs.Add (TypeCode.Set (td, resolver));
			}

			var dependsChanged = typeTCs.Any (monitorTC.AllDependencies.Contains);

			if (!dependsChanged)
				return;

			var code = monitorTC.GetLinkedCode ();

			//
			// Send the code to the device
			//
			try {
				Connect ();

				//
				// Declare it
				//
				foreach (var c in code.Declarations) {
					Console.WriteLine (c);
					if (!await EvalAsync (c, showError)) return;
				}

				//
				// Show it
				//
				Console.WriteLine (code.ValueExpression);
				if (!await EvalAsync (code.ValueExpression, showError)) return;

			} catch (Exception ex) {
				if (showError) {
					Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
				}
			}
		}

		async Task<TypeDeclaration> FindTypeAtCursor ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var resolver = await doc.GetSharedResolver ();
			var editLoc = doc.Editor.Caret.Location;
			var editTLoc = new TextLocation (editLoc.Line, editLoc.Column);
			var selTypeDecl =
				resolver.RootNode.Descendants.
				OfType<TypeDeclaration> ().
				FirstOrDefault (x => x.StartLocation <= editTLoc && editTLoc <= x.EndLocation);
			return selTypeDecl;
		}

		bool monitoring = false;
		void StartMonitoring ()
		{
			if (monitoring) return;

			IdeApp.Workbench.ActiveDocumentChanged += BindActiveDoc;
			BindActiveDoc (this, EventArgs.Empty);

			monitoring = true;
		}

		MonoDevelop.Ide.Gui.Document boundDoc = null;

		void BindActiveDoc (object sender, EventArgs e)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (boundDoc == doc) {
				return;
			}
			if (boundDoc != null) {				
				boundDoc.DocumentParsed -= ActiveDoc_DocumentParsed;
			}
			boundDoc = doc;
			if (boundDoc != null) {
				boundDoc.DocumentParsed += ActiveDoc_DocumentParsed;
			}
		}

		async void ActiveDoc_DocumentParsed (object sender, EventArgs e)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			Console.WriteLine ("DOC PARSED {0}", doc.Name);
			await VisualizeTypeAsync (showError: false);
		}

		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var doc = IdeApp.Workbench.ActiveDocument;

			info.Enabled = doc != null;
		}
	}
}

