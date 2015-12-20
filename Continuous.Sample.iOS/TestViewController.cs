using System;
using UIKit;

namespace Continuous.Sample.iOS
{
	public class TestViewController : UIViewController
	{
		public TestViewController ()
		{
			Foo ();
		}

		void Foo ()
		{
			for (var i = 3; i < 7; i++) {
				var x = 145 + i; //=148, 149, 150, 151
				x *= 2;
				var y = x * 4; //=1184, 1192, 1200, 1208
				y += 421; //=1605, 1613, 1621, 1629
				//x + y =1901, 1911, 1921, 1931
				//x / 1000.0 =0.296, 0.298, 0.3, 0.302


				//true =True, True, True, True
				Console.WriteLine (x);
			}
		}
	}
}

