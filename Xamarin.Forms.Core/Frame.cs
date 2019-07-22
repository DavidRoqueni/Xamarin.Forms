using System;
using System.ComponentModel;
using Xamarin.Forms.Platform;

namespace Xamarin.Forms
{
	[ContentProperty("Content")]
	[RenderWith(typeof(_FrameRenderer))]
	public class Frame : ContentView, IElementConfiguration<Frame>, IPaddingElement, IBorderElement
	{
		[Obsolete("OutlineColorProperty is obsolete as of version 3.0.0. Please use BorderColorProperty instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static readonly BindableProperty OutlineColorProperty = BorderElement.BorderColorProperty;

		public static readonly BindableProperty BorderColorProperty = BorderElement.BorderColorProperty;

		public static readonly BindableProperty HasShadowProperty = BindableProperty.Create("HasShadow", typeof(bool), typeof(Frame), true);

		public static readonly BindableProperty ShadowBlurProperty = BindableProperty.Create(nameof(ShadowBlur), typeof(double), typeof(Frame), 4.0d);

		public static readonly BindableProperty ShadowColorProperty = BindableProperty.Create(nameof(ShadowColor), typeof(Color), typeof(Frame), Color.Black);

		public static readonly BindableProperty ShadowOpacityProperty = BindableProperty.Create(nameof(ShadowOpacity), typeof(double), typeof(Frame), 0.8d,
									validateValue: (bindable,value) => ((double)value >=0d) && ((double)value <= 1d));

		public static readonly BindableProperty ShadowOffsetXProperty = BindableProperty.Create(nameof(ShadowOffsetX), typeof(float), typeof(Frame), 0.0f);

		public static readonly BindableProperty ShadowOffsetYProperty = BindableProperty.Create(nameof(ShadowOffsetY), typeof(float), typeof(Frame), 0.0f);

		public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(CornerRadius), typeof(float), typeof(Frame), -1.0f,
									validateValue: (bindable, value) => ((float)value) == -1.0f || ((float)value) >= 0f);

		readonly Lazy<PlatformConfigurationRegistry<Frame>> _platformConfigurationRegistry;

		public Frame()
		{
			_platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<Frame>>(() => new PlatformConfigurationRegistry<Frame>(this));
		}

		Thickness IPaddingElement.PaddingDefaultValueCreator()
		{
			return 20d;
		}

		public bool HasShadow
		{
			get { return (bool)GetValue(HasShadowProperty); }
			set { SetValue(HasShadowProperty, value); }
		}

		public double ShadowBlur
		{
			get { return (double)GetValue(ShadowBlurProperty); }
			set { SetValue(ShadowBlurProperty, value); }
		}

		public Color ShadowColor
		{
			get { return (Color)GetValue(ShadowColorProperty); }
			set { SetValue(ShadowColorProperty, value); }
		}

		public double ShadowOpacity
		{
			get { return (double)GetValue(ShadowOpacityProperty); }
			set { SetValue(ShadowOpacityProperty, value); }
		}
		public float ShadowOffsetX
		{
			get { return (float)GetValue(ShadowOffsetXProperty); }
			set { SetValue(ShadowOffsetXProperty, value); }
		}
		public float ShadowOffsetY
		{
			get { return (float)GetValue(ShadowOffsetYProperty); }
			set { SetValue(ShadowOffsetYProperty, value); }
		}

		[Obsolete("OutlineColor is obsolete as of version 3.0.0. Please use BorderColor instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public Color OutlineColor
		{
			get { return (Color)GetValue(OutlineColorProperty); }
			set { SetValue(OutlineColorProperty, value); }
		}

		public Color BorderColor
		{
			get { return (Color)GetValue(BorderElement.BorderColorProperty); }
			set { SetValue(BorderElement.BorderColorProperty, value); }
		}

		public float CornerRadius
		{
			get { return (float)GetValue(CornerRadiusProperty); }
			set { SetValue(CornerRadiusProperty, value); }
		}

		int IBorderElement.CornerRadius => (int)CornerRadius;

		// not currently used by frame
		double IBorderElement.BorderWidth => -1d;

		int IBorderElement.CornerRadiusDefaultValue => (int)CornerRadiusProperty.DefaultValue;

		Color IBorderElement.BorderColorDefaultValue => (Color)BorderColorProperty.DefaultValue;

		double IBorderElement.BorderWidthDefaultValue => -1d;

		public IPlatformElementConfiguration<T, Frame> On<T>() where T : IConfigPlatform
		{
			return _platformConfigurationRegistry.Value.On<T>();
		}

		void IBorderElement.OnBorderColorPropertyChanged(Color oldValue, Color newValue)
		{
#pragma warning disable 0618 // retain until OutlineColor removed
			OnPropertyChanged(nameof(OutlineColor));
#pragma warning restore
		}

		bool IBorderElement.IsCornerRadiusSet() => IsSet(CornerRadiusProperty);

		bool IBorderElement.IsBackgroundColorSet() => IsSet(BackgroundColorProperty);

		bool IBorderElement.IsBorderColorSet() => IsSet(BorderColorProperty);

		bool IBorderElement.IsBorderWidthSet() => false;
	}
}