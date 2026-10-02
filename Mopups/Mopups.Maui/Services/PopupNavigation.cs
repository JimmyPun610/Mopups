using AsyncAwaitBestPractices;
using Mopups.Animations;
using Mopups.Contracts;
using Mopups.Events;
using Mopups.Pages;
using Mopups.Platorms.Android.Impl;

namespace Mopups.Services;

public class PopupNavigation : IPopupNavigation
{
    private static readonly Lazy<IPopupPlatform> LazyImplementation = new(GeneratePopupPlatform, LazyThreadSafetyMode.PublicationOnly);
    private readonly Lock _locker = new();

    private readonly IPopupPlatform _popupPlatform = LazyImplementation.Value;
    private readonly List<PopupPage> _popupStack = [];

    public IReadOnlyList<PopupPage> PopupStack => _popupStack;

    public event EventHandler<PopupNavigationEventArgs>? Pushing;
    public event EventHandler<PopupNavigationEventArgs>? Pushed;
    public event EventHandler<PopupNavigationEventArgs>? Popping;
    public event EventHandler<PopupNavigationEventArgs>? Popped;

    private static IPopupPlatform GeneratePopupPlatform()
    {
        return PullPlatformImplementation();

        static IPopupPlatform PullPlatformImplementation()
        {
#if ANDROID
            return new AndroidMopups();
#elif IOS
            return new Mopups.iOS.Implementation.iOSMopups();
#endif
            throw new PlatformNotSupportedException();
        }
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
        if (_popupStack.Count > 0)
        {
            PopAllAsync().SafeFireAndForget();
        }
    }

    public Task PushAsync(PopupPage page, bool animate = true)
    {
        animate = animate && AnimationHelper.SystemAnimationsEnabled;

        Pushing?.Invoke(this, new PopupNavigationEventArgs(page, animate));
        _popupStack.Add(page);

        return MainThread.IsMainThread
            ? PushPage()
            : MainThread.InvokeOnMainThreadAsync(PushPage);

        async Task PushPage()
        {
            page.PreparingAnimation();
            await _popupPlatform.AddAsync(page);

            //Hack to make the popup to render within safe area
            if (page.HasSystemPadding)
            {
                page.Padding = new Thickness(page.SystemPadding.Left, page.SystemPadding.Top, page.SystemPadding.Right, page.SystemPadding.Bottom);
            }

            page.SendAppearing();
            await page.AppearingAnimation();
            Pushed?.Invoke(this, new PopupNavigationEventArgs(page, animate));
        };
    }

    public async Task PopAllAsync(bool animate = true)
    {
		animate = animate && AnimationHelper.SystemAnimationsEnabled;

		while (MopupService.Instance.PopupStack.Count > 0)
        {
            await PopAsync(animate);
        }
    }

    public Task PopAsync(bool animate = true)
    {
		animate = animate && AnimationHelper.SystemAnimationsEnabled;

		return _popupStack.Count <= 0
            ? throw new InvalidOperationException("PopupStack is empty")
            : RemovePageAsync(PopupStack[^1], animate);
    }

    public Task RemovePageAsync(PopupPage? page, bool animate = true)
    {
		animate = animate && AnimationHelper.SystemAnimationsEnabled;

		if (page is null)
            throw new InvalidOperationException("Page can not be null");

        if (_popupStack.Contains(page) == false)
            throw new InvalidOperationException("The page has not been pushed yet or has been removed already");

        return (MainThread.IsMainThread
            ? RemovePage()
            : MainThread.InvokeOnMainThreadAsync(RemovePage));

        async Task RemovePage()
        {
            lock (_locker)
            {
                if (!_popupStack.Contains(page))
                {
                    return;
                }
            }

            Popping?.Invoke(this, new PopupNavigationEventArgs(page, animate));
            await page.DisappearingAnimation();
            page.SendDisappearing();
            await _popupPlatform.RemoveAsync(page);
            page.DisposingAnimation();

            _popupStack.Remove(page);
            Popped?.Invoke(this, new PopupNavigationEventArgs(page, animate));
        }
    }
}

