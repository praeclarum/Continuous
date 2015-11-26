using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveCode.Client.Test
{
	[TestFixture]
	public class UsingsTest
	{
		readonly IEnumerable<string> nousings = Enumerable.Empty<string>();
		readonly IEnumerable<string> nodeps = Enumerable.Empty<string>();

		[Test]
		public void Zero ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps, "N");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("\nnamespace N{acode1}", lib.Declarations);

		}

		[Test]
		public void One ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", new []{
				"using System;",
			}, "acode1", nodeps, "N");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("using System;\nnamespace N{acode1}", lib.Declarations);

		}

		[Test]
		public void Two ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", new []{
				"using System;",
				"using System.Collections.Generic;",
			}, "acode1", nodeps, "N");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("using System;\nusing System.Collections.Generic;\nnamespace N{acode1}", lib.Declarations);

		}
	}
}

