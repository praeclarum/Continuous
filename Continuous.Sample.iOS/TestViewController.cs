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
			for (var i = 0; i < 100; i++) {
				var x = 100 + i; //= ...85, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199
				//199*2*4= ..., 1592, 1592, 1592, 1592, 1592, 1592, 1592, 1592, 1592, 1592, 1592, 1592
				x *= 2;
				var y = x * 4; //= ..., 1504, 1512, 1520, 1528, 1536, 1544, 1552, 1560, 1568, 1576, 1584, 1592
				y += 1000000; //= ..., 1001536, 1001544, 1001552, 1001560, 1001568, 1001576, 1001584, 1001592
				//x + y = ..., 1001920, 1001930, 1001940, 1001950, 1001960, 1001970, 1001980, 1001990
				//x / 1000.0 = ....378, 0.38, 0.382, 0.384, 0.386, 0.388, 0.39, 0.392, 0.394, 0.396, 0.398
				//i= ..., 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99


				//true = ..., True, True, True, True, True, True, True, True, True, True, True, True
				Console.WriteLine (x);

//				await Task.Delay (1000);
			}
		}
	}
}

