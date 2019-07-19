using System.ComponentModel;
using System.Drawing;
using AppKit;
using CoreGraphics;

namespace Xamarin.Forms.Platform.MacOS
{
	public class FrameRenderer : VisualElementRenderer<Frame>
	{
		protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
				SetupLayer();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName ||
			    e.PropertyName == Xamarin.Forms.Frame.BorderColorProperty.PropertyName ||
				e.PropertyName == Xamarin.Forms.Frame.HasShadowProperty.PropertyName ||
				e.PropertyName == Xamarin.Forms.Frame.CornerRadiusProperty.PropertyName ||
				e.PropertyName == Xamarin.Forms.Frame.ShadowBlurProperty.PropertyName ||
				e.PropertyName == Xamarin.Forms.Frame.ShadowColorProperty.PropertyName ||
				e.PropertyName == Xamarin.Forms.Frame.ShadowOffsetXProperty.PropertyName ||
				e.PropertyName == Xamarin.Forms.Frame.ShadowOffsetYProperty.PropertyName ||
				e.PropertyName == Xamarin.Forms.Frame.ShadowOpacityProperty.PropertyName)
				SetupLayer();
		}

		void SetupLayer()
		{
			Layer.CornerRadius = 5;
			if (Element.BackgroundColor == Color.Default)
				Layer.BackgroundColor = NSColor.White.CGColor;
			else
				Layer.BackgroundColor = Element.BackgroundColor.ToCGColor();

			if (Element.HasShadow)
			{
				Layer.ShadowRadius = (float)Element.ShadowBlur;
				Layer.ShadowColor = Element.ShadowColor.ToCGColor();
				Layer.ShadowOpacity = (float)Element.ShadowOpacity;
				Layer.ShadowOffset = new CGSize(Element.ShadowOffsetX,Element.ShadowOffsetY);
				Layer.MasksToBounds = false;
				Layer.ShadowPath = CGPath.FromRect(Layer.Bounds);
			}
			else
				Layer.ShadowOpacity = 0;

			if (Element.BorderColor == Color.Default)
				Layer.BorderColor = NSColor.Clear.CGColor;
			else
			{
				Layer.BorderColor = Element.BorderColor.ToCGColor();
				Layer.BorderWidth = 1;
			}

			Layer.RasterizationScale = NSScreen.MainScreen.BackingScaleFactor;
			Layer.ShouldRasterize = true;
		}
	}
}