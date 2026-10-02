using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.LifecycleEvents;
using Mopups.Contracts;
using Mopups.Services;
#if ANDROID
using Mopups.Pages;
using Mopups.Platforms.Android.Handler;
using Mopups.Platorms.Android.Impl;
#endif

namespace Mopups.Hosting;

/// <summary>
/// Represents application host extension, that used to configure handlers defined in Mopups.
/// </summary>
public static class AppHostBuilderExtensions
{
    /// <summary>
    /// Sets up lifecycle events, Maui handlers and registers <see cref="IPopupNavigation"/> in DI.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <param name="backPressHandler">Optional Android back-press logic to run when no popup is open.</param>
    public static MauiAppBuilder ConfigureMopups(this MauiAppBuilder builder, Action? backPressHandler = null)
    {
        builder.Services.TryAddSingleton<IPopupNavigation>(_ => MopupService.Instance);

#if ANDROID
        builder
            .ConfigureLifecycleEvents(lifecycle =>
            {
                lifecycle.AddAndroid(android =>
                    android.OnBackPressed(_ => AndroidMopups.SendBackPressed(backPressHandler)));
            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler(typeof(PopupPage), typeof(PopupPageHandler));
            });
#endif
        // iOS: no custom handler. MAUI Controls registers AddHandler<Page, PageHandler>() and resolves a
        // virtual view to its closest registered base type, so PopupPage gets PageHandler. On iOS that
        // handler exposes a PageViewController (IPlatformViewHandler.ViewController), which iOSMopups
        // embeds as a child of PopupRootViewController.

        return builder;
    }
}