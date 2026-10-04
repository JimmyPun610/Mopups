using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Nkraft.Mopups.Platforms.Android.Handler;

namespace Nkraft.Mopups.Platforms.Android.Handler;

public sealed class PopupPageHandler : PageHandler
{
    public PopupPageHandler()
    {
        SetMauiContext(MauiApplication.Current.Application.Windows[0].Handler.MauiContext);
    }

    public PopupPageHandler(IMauiContext context)
    {
        SetMauiContext(context);
    }

    protected override void ConnectHandler(ContentViewGroup platformView)
    {
        if (platformView is PopupPageRenderer popupPageRenderer)
            popupPageRenderer.PopupHandler = this;
        base.ConnectHandler(platformView);
    }

    protected override ContentViewGroup CreatePlatformView()
    {
        return new PopupPageRenderer(Context);
    }

    protected override void DisconnectHandler(ContentViewGroup platformView)
    {
        if (platformView is PopupPageRenderer popupPageRenderer)
            popupPageRenderer.PopupHandler = null;
        base.DisconnectHandler(platformView);
    }
}

