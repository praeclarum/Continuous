using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveCode.Client.Test
{
	[TestFixture]
	public class LinkerTest
	{
		readonly IEnumerable<string> nousings = Enumerable.Empty<string>();
		readonly IEnumerable<string> nodeps = Enumerable.Empty<string>();

		[Test]
		public void NoDeps ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps);
			var lib = a.GetLinkedCode ();
			Assert.AreEqual (1, lib.Types.Length);
		}

		[Test]
		public void DepNoChange ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new[]{"A","B","C"});
			var b = TypeCode.Set ("B", nousings, "bcode1", new[]{"B"});
			var c = TypeCode.Set ("C", nousings, "ccode1", new[]{"C"});
			var lib = a.GetLinkedCode ();
			Assert.AreEqual (1, lib.Types.Length);
		}

		[Test]
		public void DepChange ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new[]{"A","B","C"});
			var b = TypeCode.Set ("B", nousings, "bcode1", new[]{"B"});
			var c = TypeCode.Set ("C", nousings, "ccode1", new[]{"C"});
			TypeCode.Set ("B", nousings, "bcode2", new[]{"B"});
			var lib = a.GetLinkedCode ();
			Assert.AreEqual (2, lib.Types.Length);
		}

		[Test]
		public void RecDepNoChange ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new[]{"A","B"});
			var b = TypeCode.Set ("B", nousings, "bcode1", new[]{"A","B"});
			var lib = a.GetLinkedCode ();
			Assert.AreEqual (2, lib.Types.Length);
		}

		[Test]
		public void DepRecNoChange ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new[]{"A","B"});
			var b = TypeCode.Set ("B", nousings, "bcode1", new[]{"B","C"});
			var c = TypeCode.Set ("C", nousings, "ccode1", new[]{"B","C"});
			var lib = a.GetLinkedCode ();
			Assert.AreEqual (1, lib.Types.Length);
		}

		[Test]
		public void DepRecChange ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new[]{"A","B"});
			var b = TypeCode.Set ("B", nousings, "bcode1", new[]{"B","C"});
			var c = TypeCode.Set ("C", nousings, "ccode1", new[]{"B","C"});
			TypeCode.Set ("C", nousings, "ccode2", new[]{"B","C"});
			var lib = a.GetLinkedCode ();
			Assert.AreEqual (3, lib.Types.Length);
		}

		[Test]
		public void FarRecDepNoChange ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new[]{"A","B"});
			var b = TypeCode.Set ("B", nousings, "bcode1", new[]{"B","C"});
			var c = TypeCode.Set ("C", nousings, "ccode1", new[]{"A","C"});
			var d = TypeCode.Set ("D", nousings, "dcode1", new[]{"D","E"});
			var lib = a.GetLinkedCode ();
			Assert.AreEqual (3, lib.Types.Length);
		}
	}
}

