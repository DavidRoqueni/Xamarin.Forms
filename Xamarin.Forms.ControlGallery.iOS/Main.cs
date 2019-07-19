using System;
using UIKit;

namespace Xamarin.Forms.ControlGallery.iOS
{
	public class Application
	{
		static void Main(string[] args)
		{
			try
			{
				UIApplication.Main(args, typeof(CustomApplication), typeof(AppDelegate));
			}
			catch (Exception e)
			{
				Console.Write(e.StackTrace);
			}
		}
	}
}
