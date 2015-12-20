using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Continuous.Client
{
	public class WatchVariable
	{
		public static readonly Regex CommentContentRe = new Regex ("(.*?)(=|==)");

		public string Id;
		public string Expression;
		public string ExplicitExpression;
		public string FilePath;
		public int FileLine;
		public int FileColumn;
	}
}

