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
			for (var i = 3; i < 5; i++) {
				var x = 155 + i; //=158, 159
				x = 420;
				var y = x * 3; //=1260, 1260
				y += 420; //=1680, 1680
				Console.WriteLine (x);
			}
		}
	}
}

