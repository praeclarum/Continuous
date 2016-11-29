using System;

namespace Continuous.Server
{
	public interface IVM
	{
		EvalResult Eval(EvalRequest code);
	}
}
