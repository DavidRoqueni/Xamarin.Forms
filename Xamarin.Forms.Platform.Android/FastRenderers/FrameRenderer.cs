using System;
using System.ComponentModel;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;

namespace Xamarin.Forms.Platform.Android.FastRenderers
{
	public class FrameRenderer : CardView, IVisualElementRenderer, IViewRenderer, ITabStop
	{
		float _defaultElevation = -1f;
		float _defaultCornerRadius = -1f;
		int? _defaultLabelFor;
		float _defaultShadowRadius = 0f;
		float _defaultShadowDistanceX = 0f;
		float _defaultShadowDistanceY = 0f;
		Paint _paint = new Paint(PaintFlags.AntiAlias) { Dither = true, FilterBitmap=true };
		Bitmap _bitmap;
		Canvas _canvas = new Canvas();
		Rect _bounds = new Rect();
		bool _invalidateShadow = true;
		bool _disposed;
		Frame _element;
		GradientDrawable _backgroundDrawable;
		AColor _color;

		VisualElementPackager _visualElementPackager;
		VisualElementTracker _visualElementTracker;
		VisualElementRenderer _visualElementRenderer;

		readonly MotionEventHelper _motionEventHelper = new MotionEventHelper();

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;
		public event EventHandler<PropertyChangedEventArgs> ElementPropertyChanged;


		public FrameRenderer(Context context) : base(context)
		{
			SetWillNotDraw(false);
			SetLayerType(LayerType.Hardware, _paint);
			_visualElementRenderer = new VisualElementRenderer(this);
		}

		[Obsolete("This constructor is obsolete as of version 2.5. Please use FrameRenderer(Context) instead.")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public FrameRenderer() : base(Forms.Context)
		{
			_visualElementRenderer = new VisualElementRenderer(this);
		}

		protected CardView Control => this;

		AView ITabStop.TabStop => this;

		protected Frame Element
		{
			get { return _element; }
			set
			{
				if (_element == value)
					return;

				Frame oldElement = _element;
				_element = value;

				OnElementChanged(new ElementChangedEventArgs<Frame>(oldElement, _element));

				_element?.SendViewInitialized(Control);
			}
		}

		VisualElement IVisualElementRenderer.Element => Element;
		ViewGroup IVisualElementRenderer.ViewGroup => this;
		AView IVisualElementRenderer.View => this;

		SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			Context context = Context;
			return new SizeRequest(new Size(context.ToPixels(20), context.ToPixels(20)));
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			var frame = element as Frame;
			if (frame == null)
				throw new ArgumentException("Element must be of type Frame");
			Element = frame;
			_motionEventHelper.UpdateElement(frame);

			if (!string.IsNullOrEmpty(Element.AutomationId))
				ContentDescription = Element.AutomationId;
		}

		void IVisualElementRenderer.SetLabelFor(int? id)
		{
			if (_defaultLabelFor == null)
				_defaultLabelFor = ViewCompat.GetLabelFor(this);

			ViewCompat.SetLabelFor(this, (int)(id ?? _defaultLabelFor));
		}

		VisualElementTracker IVisualElementRenderer.Tracker => _visualElementTracker;

		void IVisualElementRenderer.UpdateLayout()
		{
			VisualElementTracker tracker = _visualElementTracker;
			tracker?.UpdateLayout();
		}

		void IViewRenderer.MeasureExactly()
		{
			ViewRenderer.MeasureExactly(this, Element, Context);
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;

			if (disposing)
			{
				if (Element != null)
				{
					Element.PropertyChanged -= OnElementPropertyChanged;
				}

				if (_visualElementTracker != null)
				{
					_visualElementTracker.Dispose();
					_visualElementTracker = null;
				}

				if (_visualElementPackager != null)
				{
					_visualElementPackager.Dispose();
					_visualElementPackager = null;
				}
				
				if (_backgroundDrawable != null)
				{
					_backgroundDrawable.Dispose();
					_backgroundDrawable = null;
				}

				if (_visualElementRenderer != null)
				{
					_visualElementRenderer.Dispose();
					_visualElementRenderer = null;
				}

				int count = ChildCount;
				for (var i = 0; i < count; i++)
				{
					AView child = GetChildAt(i);
					child.Dispose();
				}

				if (Element != null)
				{
					if (Platform.GetRenderer(Element) == this)
						Element.ClearValue(Platform.RendererProperty);
				}
			}

			base.Dispose(disposing);
		}

		protected virtual void OnElementChanged(ElementChangedEventArgs<Frame> e)
		{
			ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));

			if (e.OldElement != null)
			{
				e.OldElement.PropertyChanged -= OnElementPropertyChanged;
			}

			if (e.NewElement != null)
			{
				this.EnsureId();
				_backgroundDrawable = new GradientDrawable();
				_backgroundDrawable.SetShape(ShapeType.Rectangle);
				this.SetBackground(_backgroundDrawable);

				if (_visualElementTracker == null)
				{
					_visualElementTracker = new VisualElementTracker(this);
					_visualElementPackager = new VisualElementPackager(this);
					_visualElementPackager.Load();
				}

				e.NewElement.PropertyChanged += OnElementPropertyChanged;
				UpdateShadow();
				UpdateBackgroundColor();
				UpdateCornerRadius();
				UpdateBorderColor();
				UpdateClippedToBounds();

				ElevationHelper.SetElevation(this, e.NewElement);
			}
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			if (Element == null)
				return;

			var children = ((IElementController)Element).LogicalChildren;
			for (var i = 0; i < children.Count; i++)
			{
				var visualElement = children[i] as VisualElement;
				if (visualElement == null)
					continue;
				IVisualElementRenderer renderer = Android.Platform.GetRenderer(visualElement);
				renderer?.UpdateLayout();
			}
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (_visualElementRenderer.OnTouchEvent(e) || base.OnTouchEvent(e))
			{
				return true;
			}

			return _motionEventHelper.HandleMotionEvent(Parent, e);
		}

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			ElementPropertyChanged?.Invoke(this, e);

			if (e.PropertyName == Frame.HasShadowProperty.PropertyName)
				UpdateShadow();
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor();
			else if (e.PropertyName == Frame.CornerRadiusProperty.PropertyName)
				UpdateCornerRadius();
			else if (e.PropertyName == Frame.BorderColorProperty.PropertyName)
				UpdateBorderColor();
			else if (e.Is(Xamarin.Forms.Layout.IsClippedToBoundsProperty))
				UpdateClippedToBounds();
		}

		void CreateColor()
		{
			_color = AColor.Argb(
				(int)Element.ShadowOpacity * 255,
				(int)Element.ShadowColor.R * 255,
				(int)Element.ShadowColor.G * 255,
				(int)Element.ShadowColor.B * 255);
		}

		void UpdateClippedToBounds() => this.SetClipToOutline(Element.IsClippedToBounds);

		void UpdateBackgroundColor()
		{
			if (_disposed)
				return;
				
			Color bgColor = Element.BackgroundColor;
			_backgroundDrawable.SetColor(bgColor.IsDefault ? AColor.White : bgColor.ToAndroid());
		}

		void UpdateBorderColor()
		{
			if (_disposed)
				return;

			Color borderColor = Element.BorderColor;

			if (borderColor.IsDefault)
				_backgroundDrawable.SetStroke(0, AColor.Transparent);
			else
				_backgroundDrawable.SetStroke(3, borderColor.ToAndroid());
		}

		void UpdateShadow()
		{
			if (_disposed)
				return;

			float elevation = _defaultElevation;

			if (elevation == -1f)
				_defaultElevation = elevation = CardElevation;

			if (Element.HasShadow)
				CardElevation = elevation;
			else
				CardElevation = 0f;
		}

		void UpdateCornerRadius()
		{
			if (_disposed)
				return;
				
			if (_defaultCornerRadius == -1f)
			{
				_defaultCornerRadius = Radius;
			}

			float cornerRadius = Element.CornerRadius;

			if (cornerRadius == -1f)
				cornerRadius = _defaultCornerRadius;
			else
				cornerRadius = Context.ToPixels(cornerRadius);

			_backgroundDrawable.SetCornerRadius(cornerRadius);
		}

		protected override void DispatchDraw(Canvas canvas)
		{
			if (Element.HasShadow &&
				(Element.ShadowOpacity != .8f
				|| Element.ShadowColor != Color.Black
				|| Element.ShadowBlur != 0f
				|| Element.ShadowOffsetX != 0f
				|| Element.ShadowOffsetY != 0f))
			{
				if (_invalidateShadow)
				{
					if (_bounds.Width() != 0 && _bounds.Height() != 0)
					{
						_bitmap = Bitmap.CreateBitmap(_bounds.Width(), _bounds.Height(), Bitmap.Config.Argb8888);
						_canvas.SetBitmap(_bitmap);
						_invalidateShadow = false;
						base.DispatchDraw(_canvas);
						Bitmap extractedAlpha = _bitmap.ExtractAlpha();
						_canvas.DrawColor(AColor.Transparent, PorterDuff.Mode.Clear);
						_paint.Color = _color;
						_canvas.DrawBitmap(extractedAlpha, Element.ShadowOffsetX, Element.ShadowOffsetY, _paint);
						extractedAlpha.Recycle();
					}
					else
					{
						_bitmap = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Rgb565);
					}
				}
				_color.A = 255;
				_paint.Color = _color;
				if (canvas != null && _bitmap != null && !_bitmap.IsRecycled)
					canvas.DrawBitmap(_bitmap, 0.0f, 0.0f, _paint);
			}
			base.DispatchDraw(canvas);
		}

		protected override void OnDetachedFromWindow()
		{
			base.OnDetachedFromWindow();
			if (_bitmap != null)
			{
				_bitmap.Recycle();
				_bitmap = null;
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			_bounds.Set(0, 0, MeasuredWidth, MeasuredHeight);
		}

		public override void RequestLayout()
		{
			_invalidateShadow = true;
			base.RequestLayout();
		}
	}
}
