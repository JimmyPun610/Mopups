using Android.Views;
using Android.Widget;
using AndroidX.Activity;
using AndroidX.Fragment.App;
using Microsoft.Maui.Platform;
using Mopups.Contracts;
using Mopups.Extensions;
using Mopups.Pages;
using Mopups.Platforms.Android.Handler;
using Mopups.Services;
using Nkraft.CrossUtility.Extensions;
using View = Android.Views.View;

namespace Mopups.Platorms.Android.Impl;

public class AndroidMopups : IPopupPlatform
{
    private static IList<FrameLayout?> DecorViews => GetAllFragmentDecorViews();
    private static FrameLayout? DecorView => GetTopFragmentDecorView();

    public static bool SendBackPressed(Action? backPressedHandler = null)
    {
        var popupNavigationInstance = MopupService.Instance;

        if (popupNavigationInstance.PopupStack.Count > 0)
        {
            var lastPage = popupNavigationInstance.PopupStack[^1];

            var isPreventClose = lastPage.SendBackButtonPressed();

            if(!isPreventClose)
            {
                popupNavigationInstance.PopAsync().FireAndForget();
            }

            return true;
        }

        backPressedHandler?.Invoke();

        return false;
    }

    public Task AddAsync(PopupPage page)
    {
        HandleAccessibility(true, page.DisableAndroidAccessibilityHandling, page);

        page.Parent = IPlatformApplication.Current?.Application.Windows[0].Content as Element;
        //var mainPage = (Element)MauiApplication.Current.Application.Windows[0].Content;
        //mainPage.AddLogicalChild(page);

        if (page.Parent?.FindMauiContext() is { } context)
        {
            var handler = page.Handler ??= new PopupPageHandler(context);

            var androidNativeView = handler.PlatformView as View;
            DecorView?.AddView(androidNativeView);

            return PostAsync(androidNativeView);
        }
        
        return Task.CompletedTask;
    }
    
    public Task RemoveAsync(PopupPage page)
    {
        var renderer = IPopupPlatform.GetOrCreateHandler<PopupPageHandler>(page);

        HandleAccessibility(false, page.DisableAndroidAccessibilityHandling, page);

        foreach (var decoreView in DecorViews)
        {
            decoreView?.RemoveView(renderer.PlatformView as View);
        }
        renderer.DisconnectHandler(); //?? no clue if works
        page.Parent = null;

        return PostAsync(DecorView);
    }

    //! important keeps reference to pages that accessibility has applied to. This is so accessibility can be removed properly when popup is removed. #https://github.com/LuckyDucko/Mopups/issues/93
    private readonly Dictionary<Type, List<View>> _accessibilityStates = new();

    private void HandleAccessibility(bool showPopup, bool disableAccessibilityHandling, PopupPage popup)
    {
        if(disableAccessibilityHandling)
        {
            return;
        }

        if(showPopup)
        {
            var mainPage = popup.Parent as Page ?? Application.Current?.Windows[0].Page;

            if(mainPage is null)
            {
                return;
            }

            List<View> views = [];

            if(mainPage.Handler?.PlatformView is View mainPageAndroidView && mainPageAndroidView.ImportantForAccessibility != ImportantForAccessibility.NoHideDescendants)
            {
                views.Add(mainPageAndroidView);
            }

            var navCount = mainPage.Navigation.NavigationStack.Count;
            if(navCount > 0)
            {
                if(mainPage.Navigation.NavigationStack[navCount - 1]?.Handler?.PlatformView is View androidView && androidView.ImportantForAccessibility != ImportantForAccessibility.NoHideDescendants)
                {
                    views.Add(androidView);
                }
            }

            var modalCount = mainPage.Navigation.ModalStack.Count;
            if(modalCount > 0)
            {
                if(mainPage.Navigation.ModalStack[modalCount - 1]?.Handler?.PlatformView is View androidView && androidView.ImportantForAccessibility != ImportantForAccessibility.NoHideDescendants)
                {
                    views.Add(androidView);
                }
            }

            var popupCount = MopupService.Instance.PopupStack.Count;
            if(popupCount > 1)
            {
                if(MopupService.Instance.PopupStack[popupCount - 2]?.Handler?.PlatformView is View androidView && androidView.ImportantForAccessibility != ImportantForAccessibility.NoHideDescendants)
                {
                    views.Add(androidView);
                }
            }
            
            _accessibilityStates.Add(popup.GetType(), views);
        }

        if(_accessibilityStates.ContainsKey(popup.GetType()))
        {
            foreach(var view in _accessibilityStates[popup.GetType()])
            {
                ProcessView(showPopup, view);
            }

            if(!showPopup)
            {
                _accessibilityStates.Remove(popup.GetType());
            }
        }

        static void ProcessView(bool showPopup, View? view)
        {
            if(view is null)
            {
                return;
            }

            // Screen reader
            view.ImportantForAccessibility = showPopup ? ImportantForAccessibility.NoHideDescendants : ImportantForAccessibility.Auto;

            // Keyboard navigation
            ((ViewGroup)view).DescendantFocusability = showPopup ? DescendantFocusability.BlockDescendants : DescendantFocusability.AfterDescendants;
            view.ClearFocus();
        }
    }

    static Task<bool> PostAsync(View? nativeView)
    {
        if(nativeView == null)
        {
            return Task.FromResult(true);
        }

        var tcs = new TaskCompletionSource<bool>();

        nativeView.Post(() => tcs.SetResult(true));

        return tcs.Task;
    }
    
    static FrameLayout? GetTopFragmentDecorView()
    {
        if (Platform.CurrentActivity is not ComponentActivity componentActivity)
        {
            return null;
        }

        var fragments = componentActivity.GetFragmentManager()?.Fragments;
        
        if (fragments is null || !fragments.Any())
        {
            return Platform.CurrentActivity?.Window?.DecorView as FrameLayout;;
        }

        var topFragment = fragments[^1];

        if (topFragment is DialogFragment dialogFragment)
        {
            return dialogFragment.Dialog?.Window?.DecorView as FrameLayout;
        }

        return topFragment.Activity?.Window?.DecorView as FrameLayout;
    }

    private static IList<FrameLayout?> GetAllFragmentDecorViews()
    {
        IList<FrameLayout?> decorViews = new List<FrameLayout?>();
        if (Platform.CurrentActivity is not ComponentActivity componentActivity)
        {
            return decorViews;
        }

        var fragments = componentActivity.GetFragmentManager()?.Fragments;

        if (fragments is null || !fragments.Any())
        {
            decorViews.Add(Platform.CurrentActivity?.Window?.DecorView as FrameLayout);
            return decorViews;
        }

        foreach (var fragment in fragments)
        {
            if (fragment is DialogFragment dialogFragment)
            {
                decorViews.Add(dialogFragment.Dialog?.Window?.DecorView as FrameLayout);
                continue;
            }

            decorViews.Add(fragment.Activity?.Window?.DecorView as FrameLayout);
        }

        return decorViews;
    }
}
