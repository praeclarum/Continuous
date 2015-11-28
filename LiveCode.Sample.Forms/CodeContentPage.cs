using System;

using Xamarin.Forms;

namespace LiveCode.Sample.Forms
{
	public class CodeContentPage : ContentPage
	{
		public CodeContentPage ()
		{
			Content = new StackLayout { 
				Children = {
					new Label { Text = "Hello ContentPage" }
				}
			};
		}
	}
}


