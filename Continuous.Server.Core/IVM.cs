using System;
using System.Threading;
using System.Threading.Tasks;

namespace Continuous.Server
{
	public interface IVM
	{
		EvalResult Eval(EvalRequest code, TaskScheduler mainScheduler, CancellationToken token);
	}
}
