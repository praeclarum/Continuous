using System;
using System.Threading.Tasks;
using System.Linq;

#if MONODEVELOP
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Gtk;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
#endif

namespace Continuous.Client
{
	public class ContinuousEnv
	{
		public static readonly ContinuousEnv Shared = new ContinuousEnv ();

		public string MonitorTypeName = "";

		HttpClient conn = null;
		void Connect ()
		{
			if (conn == null) {
				conn = new HttpClient (new Uri ("http://127.0.0.1:" + Http.DefaultPort));
			}
		}

		public void Alert (string format, params object[] args)
		{
			Log (format, args);
			#if MONODEVELOP
			var parentWindow = IdeApp.Workbench.RootWindow;
			var dialog = new MessageDialog(parentWindow, DialogFlags.DestroyWithParent,
				MessageType.Info, ButtonsType.Ok,
				false,
				format, args);
			dialog.Run ();
			dialog.Destroy ();
			#endif
		}

		async Task<bool> EvalAsync (string code, bool showError)
		{
			Connect ();
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

		public async Task StopVisualizingAsync ()
		{
			MonitorTypeName = "";
			TypeCode.Clear ();
			try {
				Connect ();
				await conn.StopVisualizingAsync ();
			} catch (Exception ex) {
				Log ("ERROR: {0}", ex);
			}
		}

		public event Action<LinkedCode> LinkedMonitoredCode = delegate {};

		#if MONODEVELOP
		public async Task VisualizeAsync ()
		{
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

			MonitorTypeName = typeName;
			//			monitorNamespace = nsName;

			await SetTypesAndVisualizeMonitoredTypeAsync (forceEval: true, showError: true);
		}

		LinkedCode lastLinkedCode = null;

		async Task SetTypesAndVisualizeMonitoredTypeAsync (bool forceEval, bool showError)
		{
			//
			// Gobble up all we can about the types in the active document
			//
			var typeDecls = await GetTopLevelTypeDeclsAsync ();
			foreach (var td in typeDecls) {
				td.SetTypeCode ();
			}

			await VisualizeMonitoredTypeAsync (forceEval, showError);
		}

		public async Task VisualizeMonitoredTypeAsync (bool forceEval, bool showError)
		{
			//
			// Refresh the monitored type
			//
			if (string.IsNullOrWhiteSpace (MonitorTypeName))
				return;

			var monitorTC = TypeCode.Get (MonitorTypeName);

			var code = await Task.Run (() => monitorTC.GetLinkedCode ());

			LinkedMonitoredCode (code);

			if (!forceEval && lastLinkedCode != null && lastLinkedCode.CacheKey == code.CacheKey) {
				return;
			}

			//
			// Send the code to the device
			//
			try {
				//
				// Declare it
				//
				Log (code.Declarations);
				if (!await EvalAsync (code.Declarations, showError)) return;

				//
				// Show it
				//
				Log (code.ValueExpression);
				if (!await EvalAsync (code.ValueExpression, showError)) return;

				//
				// If we made it this far, remember so we don't re-send the same
				// thing immediately
				//
				lastLinkedCode = code;

			} catch (Exception ex) {
				if (showError) {
					Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
				}
			}
		}

		abstract class TypeDecl
		{
			public abstract string Name { get; }
			public abstract TextLocation StartLocation { get; }
			public abstract TextLocation EndLocation { get; }
			public abstract void SetTypeCode ();
		}

		class CSharpTypeDecl : TypeDecl
		{
			public TypeDeclaration Declaration;
			public CSharpAstResolver Resolver;
			public override string Name {
				get {
					return Declaration.Name;
				}
			}
			public override TextLocation StartLocation {
				get {
					return Declaration.StartLocation;
				}
			}
			public override TextLocation EndLocation {
				get {
					return Declaration.EndLocation;
				}
			}
			public override void SetTypeCode ()
			{
				TypeCode.Set (Declaration, Resolver);
			}
		}

		class XamlTypeDecl : TypeDecl
		{
			public string XamlText;
			public override string Name {
				get {
					Console.WriteLine ("XAML TYPE GET NAME");
					return "??";
				}
			}
			public override TextLocation StartLocation {
				get {
					return new TextLocation (TextLocation.MinLine, TextLocation.MinColumn);
				}
			}
			public override TextLocation EndLocation {
				get {
					return new TextLocation (1000000, TextLocation.MinColumn);
				}
			}
			public override void SetTypeCode ()
			{
				Console.WriteLine ("SET XAML TYPE CODE");
			}
		}

		async Task<TypeDecl[]> GetTopLevelTypeDeclsAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			Log ("Doc = {0}", doc);
			if (doc == null) {
				return new TypeDecl[0];
			}

			var ext = doc.FileName.Extension;

			if (ext == ".cs") {
				var resolver = await doc.GetSharedResolver ();
				var typeDecls =
					resolver.RootNode.Descendants.
					OfType<TypeDeclaration> ().
					Where (x => !(x.Parent is TypeDeclaration)).
					Select (x => new CSharpTypeDecl {
						Declaration = x,
						Resolver = resolver,
					});
				return typeDecls.ToArray ();
			}

			if (ext == ".xaml") {
				var xaml = doc.Editor.Text;
				return new TypeDecl[] {
					new XamlTypeDecl {
						XamlText = xaml,
					},
				};
			}

			return new TypeDecl[0];
		}

		async Task<TypeDecl> FindTypeAtCursor ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null) {
				return null;
			}

			var editLoc = doc.Editor.Caret.Location;
			var editTLoc = new TextLocation (editLoc.Line, editLoc.Column);

			var selTypeDecl =
				(await GetTopLevelTypeDeclsAsync ()).
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
			Log ("DOC PARSED {0}", doc.Name);
			await SetTypesAndVisualizeMonitoredTypeAsync (forceEval: false, showError: false);
		}

		public async Task VisualizeSelectionAsync ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;

			if (doc != null) {
				var code = doc.Editor.SelectedText;

				try {
					await EvalAsync (code, showError: true);
				} catch (Exception ex) {
					Alert ("Could not communicate with the app.\n\n{0}: {1}", ex.GetType (), ex.Message);
				}
			}
		}

		#endif

		protected void Log (string format, params object[] args)
		{
			#if DEBUG
			Log (string.Format (format, args));
			#endif
		}

		protected void Log (string msg)
		{
			#if DEBUG
			Console.WriteLine (msg);
			#endif
		}
	}
}

