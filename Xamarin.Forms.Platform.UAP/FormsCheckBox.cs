﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using WindowsCheckbox = Windows.UI.Xaml.Controls.CheckBox;


namespace Xamarin.Forms.Platform.UWP
{
	public class FormsCheckBox : WindowsCheckbox
	{

		public static readonly DependencyProperty TintBrushProperty =
			DependencyProperty.Register(nameof(TintBrush), typeof(Brush), typeof(FormsCheckBox),
				new PropertyMetadata(default(Brush)));

		public FormsCheckBox()
		{
			
		}

		public Brush TintBrush
		{
			get { return (Brush)GetValue(TintBrushProperty); }
			set { SetValue(TintBrushProperty, value); }
		}
	}
}
