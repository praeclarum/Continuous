using System;
using UIKit;
using System.Threading.Tasks;

namespace Continuous.Sample.iOS
{
	public class TestViewController : UIViewController
	{
		public TestViewController ()
		{
			Foo ();
		}

		async void Foo ()
		{
			for (var i = 0; i < 70; i++) {
				var x = 100 + i; //=100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 1...
				x *= 2;
				var y = x * 4; //=800, 808, 816, 824, 832, 840, 848, 856, 864, 872, 880, 888, 896, 9...
				y += 1000000; //=1000800, 1000808, 1000816, 1000824, 1000832, 1000840, 1000848, 100...
				//x + y =1001000, 1001010, 1001020, 1001030, 1001040, 1001050, 100106...
				//x / 1000.0 =0.2, 0.202, 0.204, 0.206, 0.208, 0.21, 0.212, 0.214, 0....
				//i=0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18,...


				//true =True, True, True, True, True, True, True, True, True, True, T...
				Console.WriteLine (x);

//				await Task.Delay (1000);
			}
		}
	}
}

