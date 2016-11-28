using System;

namespace Continuous.Server
{
	public interface IVM
	{
		EvalResult Eval(string code);
	}
}
