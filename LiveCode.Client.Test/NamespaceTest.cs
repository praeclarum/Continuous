using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveCode.Client.Test
{
	[TestFixture]
	public class NamespaceTest
	{
		readonly IEnumerable<string> nousings = Enumerable.Empty<string>();
		readonly IEnumerable<string> nodeps = Enumerable.Empty<string>();

		[Test]
		public void Zero ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps);
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("", a.FullNamespace);
			Assert.IsTrue (lib.ValueExpression.StartsWith ("new A", StringComparison.Ordinal));
		}

		[Test]
		public void One ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps, "N");
			var lib = a.GetLinkedCode ();
			Assert.IsTrue (lib.Declarations.Contains ("namespace N"));
			Assert.IsTrue (lib.ValueExpression.StartsWith ("new N.A", StringComparison.Ordinal));
		}

		[Test]
		public void Two ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps, "N.M");
			var lib = a.GetLinkedCode ();
			Assert.IsTrue (lib.Declarations.Contains ("namespace N.M"));
			Assert.IsTrue (lib.ValueExpression.StartsWith ("new N.M.A", StringComparison.Ordinal));
		}
	}
}

