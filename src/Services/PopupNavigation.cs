using Nkraft.Mopups.Animations;
using Nkraft.Mopups.Contracts;
using Nkraft.Mopups.Events;
using Nkraft.Mopups.Pages;

#if ANDROID
using Nkraft.Mopups.Platforms.Android;
#elif IOS
using Nkraft.Mopups.Platforms.iOS;
#endif

namespace Nkraft.Mopups.Services;

public class PopupNavigation : IPopupNavigation
{
    private static readonly Lazy<IPopupPlatform> LazyImplementation = new(GeneratePopupPlatform, LazyThreadSafetyMode.PublicationOnly);
    private readonly Lock _locker = new();

    private readonly IPopupPlatform _popupPlatform = LazyImplementation.Value;
    private readonly List<PopupPage> _popupStack = [];

    private volatile PopupPage[] _snapshot = [];

    private readonly HashSet<PopupPage> _removing = [];

    public IReadOnlyList<PopupPage> PopupStack => _snapshot;

    public event EventHandler<PopupNavigationEventArgs>? Pushing;
    public event EventHandler<PopupNavigationEventArgs>? Pushed;
    public event EventHandler<PopupNavigationEventArgs>? Popping;
    public event EventHandler<PopupNavigationEventArgs>? Popped;

    private static IPopupPlatform GeneratePopupPlatform()
    {
#if ANDROID
        return new AndroidMopups();
#elif IOS
        return new iOSMopups();
#else
        throw new PlatformNotSupportedException();
#endif
    }

    public Task PushAsync(PopupPage page, bool animate = true)
    {
        ArgumentNullException.ThrowIfNull(page);
        animate = animate && AnimationHelper.SystemAnimationsEnabled;

        lock (_locker)
        {
            if (_popupStack.Contains(page))
                throw new InvalidOperationException("The page has already been pushed.");

            _popupStack.Add(page);
            _snapshot = [.. _popupStack];
        }

        Pushing?.Invoke(this, new PopupNavigationEventArgs(page, animate));

        return MainThread.IsMainThread
            ? PushPage()
            : MainThread.InvokeOnMainThreadAsync(PushPage);

        async Task PushPage()
        {
            try
            {
                page.PreparingAnimation();
                await _popupPlatform.AddAsync(page);
            }
            catch
            {
                // Don't leave a page in the stack that never made it on screen.
                lock (_locker)
                {
                    _popupStack.Remove(page);
                    _snapshot = [.. _popupStack];
                }
                throw;
            }

            // Safe-area/keyboard padding is now applied by PopupPage itself whenever the
            // platform reports new SystemPadding/KeyboardOffset values (rotation, keyboard, etc.).

            page.SendAppearing();
            await page.AppearingAnimation();
            Pushed?.Invoke(this, new PopupNavigationEventArgs(page, animate));
        }
    }

    public async Task PopAllAsync(bool animate = true)
    {
        animate = animate && AnimationHelper.SystemAnimationsEnabled;

        while (true)
        {
            PopupPage? top;
            lock (_locker)
                top = _popupStack.LastOrDefault(p => !_removing.Contains(p));

            if (top is null)
                break;

            await RemovePageAsync(top, animate);
        }
    }

    public Task PopAsync(bool animate = true)
    {
        animate = animate && AnimationHelper.SystemAnimationsEnabled;

        PopupPage? top;
        lock (_locker)
            top = _popupStack.LastOrDefault(p => !_removing.Contains(p));

        return top is null
            ? throw new InvalidOperationException("PopupStack is empty")
            : RemovePageAsync(top, animate);
    }

    /// <summary>
    /// Removes <paramref name="page"/>. Calling this for a page that is already being removed,
    /// or was never pushed, is a no-op.
    /// </summary>
    public Task RemovePageAsync(PopupPage? page, bool animate = true)
    {
        ArgumentNullException.ThrowIfNull(page);
        animate = animate && AnimationHelper.SystemAnimationsEnabled;

        lock (_locker)
        {
            if (!_popupStack.Contains(page) || !_removing.Add(page))
                return Task.CompletedTask;
        }

        return MainThread.IsMainThread
            ? RemovePage()
            : MainThread.InvokeOnMainThreadAsync(RemovePage);

        async Task RemovePage()
        {
            try
            {
                Popping?.Invoke(this, new PopupNavigationEventArgs(page, animate));
                await page.DisappearingAnimation();
                page.SendDisappearing();
                await _popupPlatform.RemoveAsync(page);
                page.DisposingAnimation();
            }
            finally
            {
                lock (_locker)
                {
                    _removing.Remove(page);
                    _popupStack.Remove(page);
                    _snapshot = [.. _popupStack];
                }
            }

            Popped?.Invoke(this, new PopupNavigationEventArgs(page, animate));
        }
    }
}