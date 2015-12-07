using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Continuous.Client.Test
{
	[TestFixture]
	public class CodeChangedTest
	{
		readonly IEnumerable<string> nousings = Enumerable.Empty<string>();
		readonly IEnumerable<string> nodeps = Enumerable.Empty<string>();

		[Test]
		public void Init ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps);
			Assert.IsFalse (a.CodeChanged);
		}

		[Test]
		public void DidntChange ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps);
			TypeCode.Set ("A", nousings, "acode1", nodeps);
			Assert.IsFalse (a.CodeChanged);
		}

		[Test]
		public void Changed ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps);
			TypeCode.Set ("A", nousings, "acode2", nodeps);
			Assert.IsTrue (a.CodeChanged);
		}

		[Test]
		public void Repeat ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps);
			TypeCode.Set ("A", nousings, "acode2", nodeps);
			TypeCode.Set ("A", nousings, "acode2", nodeps);
			Assert.IsTrue (a.CodeChanged);
		}
	}
}

