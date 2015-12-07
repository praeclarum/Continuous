using System;
using Mono.CSharp;

namespace Continuous.Server
{
	public partial class VM
	{
		partial void PlatformSettings (CompilerSettings settings)
		{
			settings.AddConditionalSymbol ("__IOS__");
		}

		partial void PlatformInit ()
		{
			object res;
			bool hasRes;
			eval.Evaluate ("using Foundation;", out res, out hasRes);
			eval.Evaluate ("using CoreGraphics;", out res, out hasRes);
			eval.Evaluate ("using UIKit;", out res, out hasRes);
		}
	}
}

