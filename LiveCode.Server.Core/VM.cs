using System;
using System.Collections.Generic;
using Mono.CSharp;

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
			Console.WriteLine ("EVAL ON THREAD {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);

			var sw = new System.Diagnostics.Stopwatch ();

			object result;
			bool hasResult;

			lock (mutex) {
				InitIfNeeded ();

				printer.Messages.Clear ();

				sw.Start ();

				eval.Evaluate (code, out result, out hasResult);

				sw.Stop ();
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
				var settings = new CompilerSettings ();
				var context = new CompilerContext (settings, printer);
				eval = new Evaluator (context);
			}
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

