using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Views;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Platform.Android.AppCompat;
using AView = Android.Views.View;
using Fragment = Android.Support.V4.App.Fragment;
using LP = Android.Views.ViewGroup.LayoutParams;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Xamarin.Forms.Platform.Android
{
	public class ShellSectionRenderer : Fragment, IShellSectionRenderer, ViewPager.IOnPageChangeListener, AView.IOnClickListener, IShellObservableFragment, IAppearanceObserver
	{
		#region IOnPageChangeListener

		void ViewPager.IOnPageChangeListener.OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
		{
		}

		void ViewPager.IOnPageChangeListener.OnPageScrollStateChanged(int state)
		{
		}

		void ViewPager.IOnPageChangeListener.OnPageSelected(int position)
		{
			if (_selecting)
				return;

			// TODO : Find a way to make this cancellable
			var shellSection = ShellSection;
			var shellContent = shellSection.Items[position];

			if (shellContent == shellSection.CurrentItem)
				return;

			var stack = shellSection.Stack.ToList();
			bool result = ((IShellController)_shellContext.Shell).ProposeNavigation(ShellNavigationSource.ShellContentChanged,
				(ShellItem)shellSection.Parent, shellSection, shellContent, stack, true);

			if (result)
			{
				UpdateCurrentItem(shellContent);
			}
			else
			{
				_selecting = true;

				// Android doesn't really appreciate you calling SetCurrentItem inside a OnPageSelected callback.
				// It wont crash but the way its programmed doesn't really anticipate re-entrancy around that method
				// and it ends up going to the wrong location. Thus we must invoke.

				Device.BeginInvokeOnMainThread(() =>
				{
					if (position < _viewPager.ChildCount && _toolbarTracker != null)
					{
						_viewPager.SetCurrentItem(position, false);
						UpdateCurrentItem(shellContent);
					}

					_selecting = false;
				});
			}
		}

		void UpdateCurrentItem(ShellContent content)
		{
			if (_toolbarTracker == null)
				return;

			var page = ((IShellContentController)content).Page;
			if (page == null)
				throw new ArgumentNullException(nameof(page), "Shell Content Page is Null");

			ShellSection.SetValueFromRenderer(ShellSection.CurrentItemProperty, content);
			_toolbarTracker.Page = page;
		}

		#endregion IOnPageChangeListener

		#region IAppearanceObserver

		void IAppearanceObserver.OnAppearanceChanged(ShellAppearance appearance)
		{
			if (appearance == null)
				ResetAppearance();
			else
				SetAppearance(appearance);
		}

		#endregion IAppearanceObserver

		#region IOnClickListener

		void AView.IOnClickListener.OnClick(AView v)
		{
		}

		#endregion IOnClickListener

		readonly IShellContext _shellContext;
		AView _rootView;
		bool _selecting;
		TabLayout _tablayout;
		IShellTabLayoutAppearanceTracker _tabLayoutAppearanceTracker;
		Toolbar _toolbar;
		IShellToolbarAppearanceTracker _toolbarAppearanceTracker;
		IShellToolbarTracker _toolbarTracker;
		ViewPager _viewPager;
		NestedScrollView _scrollview;

		public ShellSectionRenderer(IShellContext shellContext)
		{
			_shellContext = shellContext;
		}

		protected ShellSectionRenderer(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public event EventHandler AnimationFinished;

		Fragment IShellObservableFragment.Fragment => this;
		public ShellSection ShellSection { get; set; }

		public override AView OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var shellSection = ShellSection;
			if (shellSection == null)
				return null;

			var root = inflater.Inflate(Resource.Layout.RootLayout, null).JavaCast<CoordinatorLayout>();

			_toolbar = root.FindViewById<Toolbar>(Resource.Id.main_toolbar);
			_scrollview = root.FindViewById<NestedScrollView>(Resource.Id.main_scrollview);
			_tablayout = root.FindViewById<TabLayout>(Resource.Id.main_tablayout);

			_viewPager = new FormsViewPager(Context)
			{
				LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
			};

			_viewPager.AddOnPageChangeListener(this);
			_viewPager.Id = Platform.GenerateViewId();

			_viewPager.Adapter = new ShellFragmentPagerAdapter(shellSection, ChildFragmentManager);
			_viewPager.OverScrollMode = OverScrollMode.Never;

			_tablayout.SetupWithViewPager(_viewPager);

			var currentPage = ((IShellContentController)shellSection.CurrentItem).GetOrCreateContent();
			var currentIndex = ShellSection.Items.IndexOf(ShellSection.CurrentItem);

			_toolbarTracker = _shellContext.CreateTrackerForToolbar(_toolbar);
			_toolbarTracker.Page = currentPage;

			_viewPager.CurrentItem = currentIndex;
			_scrollview.AddView(_viewPager);

			if (shellSection.Items.Count == 1)
			{
				_tablayout.Visibility = ViewStates.Gone;
			}

			_tabLayoutAppearanceTracker = _shellContext.CreateTabLayoutAppearanceTracker(ShellSection);
			_toolbarAppearanceTracker = _shellContext.CreateToolbarAppearanceTracker();

			HookEvents();

			return _rootView = root;
		}

		// Use OnDestroy instead of OnDestroyView because OnDestroyView will be
		// called before the animation completes. This causes tons of tiny issues.
		public override void OnDestroy()
		{
			if (_rootView != null)
			{
				UnhookEvents();

				var adapter = _viewPager.Adapter;
				_viewPager.Adapter = null;
				adapter.Dispose();

				_viewPager.RemoveOnPageChangeListener(this);
				_scrollview.RemoveView(_viewPager);

				_toolbarAppearanceTracker.Dispose();
				_tabLayoutAppearanceTracker.Dispose();
				_toolbarTracker.Dispose();
				_tablayout.Dispose();
				_toolbar.Dispose();
				_viewPager.Dispose();
				_scrollview.Dispose();
				_rootView.Dispose();
			}

			_toolbarAppearanceTracker = null;
			_tabLayoutAppearanceTracker = null;
			_toolbarTracker = null;
			_tablayout = null;
			_toolbar = null;
			_viewPager = null;
			_scrollview = null;
			_rootView = null;

			base.OnDestroy();
		}

		protected virtual void OnAnimationFinished(EventArgs e)
		{
			AnimationFinished?.Invoke(this, e);
		}

		protected virtual void OnItemsCollectionChagned(object sender, NotifyCollectionChangedEventArgs e) =>
			_tablayout.Visibility = (ShellSection.Items.Count > 1) ? ViewStates.Visible : ViewStates.Gone;

		protected virtual void OnShellItemPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_rootView == null)
				return;

			if (e.PropertyName == ShellSection.CurrentItemProperty.PropertyName)
			{
				var newIndex = ShellSection.Items.IndexOf(ShellSection.CurrentItem);

				if (newIndex >= 0)
				{
					_viewPager.CurrentItem = newIndex;
				}
			}
		}

		protected virtual void ResetAppearance()
		{
			_toolbarAppearanceTracker.ResetAppearance(_toolbar, _toolbarTracker);
			_tabLayoutAppearanceTracker.ResetAppearance(_tablayout);
		}

		protected virtual void SetAppearance(ShellAppearance appearance)
		{
			_toolbarAppearanceTracker.SetAppearance(_toolbar, _toolbarTracker, appearance);
			_tabLayoutAppearanceTracker.SetAppearance(_tablayout, appearance);
		}

		void HookEvents()
		{
			((INotifyCollectionChanged)ShellSection.Items).CollectionChanged += OnItemsCollectionChagned;
			((IShellController)_shellContext.Shell).AddAppearanceObserver(this, ShellSection);
			ShellSection.PropertyChanged += OnShellItemPropertyChanged;
		}

		void UnhookEvents()
		{
			((INotifyCollectionChanged)ShellSection.Items).CollectionChanged -= OnItemsCollectionChagned;
			((IShellController)_shellContext.Shell).RemoveAppearanceObserver(this);
			ShellSection.PropertyChanged -= OnShellItemPropertyChanged;
		}
	}
}