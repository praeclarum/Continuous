using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Continuous.Client.Test
{
	[TestFixture]
	public class AllDependenciesTest
	{
		readonly IEnumerable<string> nousings = Enumerable.Empty<string>();
		readonly IEnumerable<string> nodeps = Enumerable.Empty<string>();

		[Test]
		public void OneLevel ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new []{ "B", "C" });
			var allDeps = a.AllDependencies;
			Assert.AreEqual (3, allDeps.Count);
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("A")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("B")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("C")));
		}

		[Test]
		public void TwoLevels ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new []{ "B", "C" });
			var b = TypeCode.Set ("B", nousings, "bcode1", new []{ "B", "C", "D" });
			var allDeps = a.AllDependencies;
			Assert.AreEqual (4, allDeps.Count);
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("A")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("B")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("C")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("D")));
		}

		[Test]
		public void Recursive ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new []{ "A", "B", "C" });
			var b = TypeCode.Set ("B", nousings, "bcode1", new []{ "A", "B", "C", "D" });
			var allDeps = a.AllDependencies;
			Assert.AreEqual (4, allDeps.Count);
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("A")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("B")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("C")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("D")));
		}

		[Test]
		public void Ignore ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", new []{ "A", "B", "C" });
			var b = TypeCode.Set ("B", nousings, "bcode1", new []{ "A", "B", "C", "D" });
			var i = TypeCode.Set ("I", nousings, "icode1", new []{ "A", "B", "C", "D" });
			var allDeps = a.AllDependencies;
			Assert.AreEqual (4, allDeps.Count);
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("A")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("B")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("C")));
			Assert.IsTrue (allDeps.Contains (TypeCode.Get ("D")));
		}


	}
}

