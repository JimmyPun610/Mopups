using Microsoft.Maui.Platform;
using Mopups.Contracts;
using Mopups.Pages;
using UIKit;

namespace Mopups.Platforms.iOS;

/// <summary>
/// Hosts each popup in its own <see cref="PopupWindow"/> stacked above the app window.
/// The popup page's view controller is embedded as a child of <see cref="PopupRootViewController"/>
/// (no modal presentation), so there is no present/dismiss dance and no artificial delays.
/// </summary>
internal sealed class iOSMopups : IPopupPlatform
{
    // Strong references are required: UIKit tears down windows nobody holds on to (upstream #459).
    private readonly Dictionary<PopupPage, PopupWindow> _windows = [];

    // Stacking order, topmost last. Used for window levels and for restoring key status.
    private readonly List<PopupWindow> _order = [];

    public Task AddAsync(PopupPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (_windows.ContainsKey(page))
            return Task.CompletedTask;

        var mauiWindow = ResolveHostWindow()
            ?? throw new InvalidOperationException("No MAUI window is available to host the popup.");

        var context = mauiWindow.Handler?.MauiContext
            ?? throw new InvalidOperationException("The host window has no MauiContext.");

        var hostWindow = mauiWindow.Handler?.PlatformView as UIWindow;
        var scene = hostWindow?.WindowScene ?? ResolveForegroundScene()
            ?? throw new InvalidOperationException("No foreground UIWindowScene is available to host the popup.");

        // 1.3.2 behaviour: set Parent only. Resources and BindingContext still inherit through Parent,
        // but the host page doesn't list the popup as a child, so the navigation toolbar never treats
        // it as the current page (hamburger/nav bar disappearing, upstream #158).
        page.Parent = mauiWindow.Page;

        var handler = (IPlatformViewHandler)page.ToHandler(context);
        var pageController = handler.ViewController
            ?? throw new InvalidOperationException("PageHandler did not produce a UIViewController.");

        var window = new PopupWindow(scene, page, hostWindow)
        {
            BackgroundColor = UIColor.Clear,
            // Above the app window, below MAUI's DisplayAlert window (UIWindowLevel.Alert + 1).
            WindowLevel = UIWindowLevel.Normal + _order.Count + 1,
            RootViewController = new PopupRootViewController(page, pageController, hostWindow),
        };

        _windows[page] = window;
        _order.Add(window);

        window.MakeKeyAndVisible();

        // Force a layout pass now so the page has a real size and SystemPadding is populated
        // before Appearing animations read Width/Height (MoveAnimation/ScaleAnimation offsets).
        window.RootViewController.View?.LayoutIfNeeded();

        return Task.CompletedTask;
    }

    public Task RemoveAsync(PopupPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (!_windows.Remove(page, out var window))
            return Task.CompletedTask;

        _order.Remove(window);
        var hostWindow = window.HostWindow;

        // Detach while handlers are still connected: removing the page view from the window
        // is what makes MAUI raise Page.Unloaded (MvvmEssentials' PageFactory listens to it).
        if (window.RootViewController is PopupRootViewController root)
        {
            root.DetachPage();
            window.RootViewController = null;
            root.Dispose();
        }

        window.Hidden = true;
        window.Dispose();

        DisconnectHandlers(page);
        page.Parent = null;

        RestoreKeyWindow(hostWindow);

        return Task.CompletedTask;
    }

    private void RestoreKeyWindow(UIWindow? hostWindow)
    {
        if (_order.Count > 0)
            _order[^1].MakeKeyWindow();
        else
            hostWindow?.MakeKeyWindow();
    }

    private static Window? ResolveHostWindow()
    {
        var windows = Application.Current?.Windows;
        if (windows is null || windows.Count == 0)
            return null;

        // Popup windows may currently be key, so fall back to the foreground scene's window.
        return windows.FirstOrDefault(w => (w.Handler?.PlatformView as UIWindow)?.IsKeyWindow == true)
            ?? windows.FirstOrDefault(w => (w.Handler?.PlatformView as UIWindow)?.WindowScene?.ActivationState
                == UISceneActivationState.ForegroundActive)
            ?? windows[0];
    }

    private static UIWindowScene? ResolveForegroundScene() =>
        UIApplication.SharedApplication.ConnectedScenes
            .ToArray()
            .OfType<UIWindowScene>()
            .Where(s => s.Session.Role == UIWindowSceneSessionRole.Application)
            .OrderByDescending(s => s.ActivationState == UISceneActivationState.ForegroundActive)
            .FirstOrDefault();

    private static void DisconnectHandlers(PopupPage page)
    {
        // Children first, then the page. Only disconnect: the native views are owned by the
        // handlers, and disposing them by hand (as the old code did) can crash on reuse.
        var descendants = page.GetVisualTreeDescendants();
        for (var i = descendants.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(descendants[i], page))
                continue;

            (descendants[i] as IElement)?.Handler?.DisconnectHandler();
        }

        page.Handler?.DisconnectHandler();
    }
}