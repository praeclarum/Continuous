using System;
using System.Collections.Generic;
using Mono.CSharp;
using System.Reflection;
using System.Diagnostics;

namespace LiveCode.Server
{
	/// <summary>
	/// Evaluates expressions using the mono C# REPL.
	/// This method is thread safe so you can call it from anywhere.
	/// </summary>
	public class VM
	{
		readonly object mutex = new object ();
		readonly Printer printer = new Printer ();

		Evaluator eval;

		public EvalResponse Eval (string code)
		{

			var sw = new System.Diagnostics.Stopwatch ();

			object result;
			bool hasResult;

			lock (mutex) {
				InitIfNeeded ();

				Debug.WriteLine ("EVAL ON THREAD {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);

				printer.Messages.Clear ();

				sw.Start ();

				eval.Evaluate (code, out result, out hasResult);

				sw.Stop ();

				Debug.WriteLine ("END EVAL ON THREAD {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
			}

			return new EvalResponse {
				Messages = printer.Messages.ToArray (),
				Duration = sw.Elapsed,
				Result = result,
				HasResult = hasResult,
			};
		}

		void InitIfNeeded()
		{
			if (eval == null) {

				Debug.WriteLine ("INIT EVAL");

				var settings = new CompilerSettings ();
				var context = new CompilerContext (settings, printer);
				eval = new Evaluator (context);

				//
				// Add References to get UIKit, etc. Also add a hook to catch dynamically loaded assemblies.
				//
				AppDomain.CurrentDomain.AssemblyLoad += (_, e) => {
					Debug.WriteLine ("DYNAMIC REF {0}", e.LoadedAssembly);
					AddReference (e.LoadedAssembly);
				};
				foreach (var a in AppDomain.CurrentDomain.GetAssemblies ()) {
					Debug.WriteLine ("STATIC REF {0}", a);
					AddReference (a);
				}
			}
		}

		void AddReference (Assembly a)
		{
			//
			// Avoid duplicates of what comes prereferenced with Mono.CSharp.Evaluator
			//
			var name = a.GetName ().Name;
			if (name == "mscorlib" || name == "System" || name == "System.Core")
				return;

			//
			// TODO: Should this lock if called from the AssemblyLoad event?
			//
			eval.ReferenceAssembly (a);
		}

		class Printer : ReportPrinter
		{
			public readonly List<EvalMessage> Messages = new List<EvalMessage> ();
			public override void Print (AbstractMessage msg, bool showFullPath)
			{
				var m = new EvalMessage {
					MessageType = msg.MessageType,
					Text = msg.Text,
					Line = msg.Location.Row,
					Column = msg.Location.Column,
				};

				Messages.Add (m);

				//
				// Print it to the console cause the console always works
				//
				var tm = msg.ToString ();
				System.Threading.ThreadPool.QueueUserWorkItem (_ =>
					Console.WriteLine (tm));
			}
		}
	}
}

