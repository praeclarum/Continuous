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
		public void ZeroUsings ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps, "N");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("namespace N{\nacode1}", lib.Declarations);

		}

		[Test]
		public void OneUsing ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", new []{
				"using System;",
			}, "acode1", nodeps, "N");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("namespace N{using System;\nacode1}", lib.Declarations);

		}

		[Test]
		public void TwoUsings ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", new []{
				"using System;",
				"using System.Collections.Generic;",
			}, "acode1", nodeps, "N");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("namespace N{using System;\nusing System.Collections.Generic;\nacode1}", lib.Declarations);

		}

		[Test]
		public void ZeroUsingsNoNamespace ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", nousings, "acode1", nodeps, "");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("\nacode1", lib.Declarations);

		}

		[Test]
		public void OneUsingNoNamespace ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", new []{
				"using System;",
			}, "acode1", nodeps, "");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("using System;\nacode1", lib.Declarations);

		}

		[Test]
		public void TwoUsingsNoNamespace ()
		{
			TypeCode.Clear ();
			var a = TypeCode.Set ("A", new []{
				"using System;",
				"using System.Collections.Generic;",
			}, "acode1", nodeps, "");
			var lib = a.GetLinkedCode ();
			Assert.AreEqual ("using System;\nusing System.Collections.Generic;\nacode1", lib.Declarations);

		}

	}
}

