using System;
using System.Collections.Generic;
using System.Linq;

namespace Continuous.Client
{
	public class WatchVariable
	{
		public Guid Id;
		public string Expression;
		public string FilePath;
		public int FileLine;
		public int FileColumn;
	}
}

