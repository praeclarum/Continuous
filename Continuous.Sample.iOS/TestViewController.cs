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
			var x = 12; //=
			x = 42;
			var y = 120;
			y = 420; //=
			Console.WriteLine (x);
		}
	}
}

