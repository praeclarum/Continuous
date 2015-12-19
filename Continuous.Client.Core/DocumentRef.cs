using System;

namespace Continuous.Client
{
	public class DocumentRef
	{
		public string FullPath { get; set; }

		public DocumentRef (string fullPath)
		{
			FullPath = fullPath;
		}
	}
}

