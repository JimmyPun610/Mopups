using CoreGraphics;
using Nkraft.Mopups.Pages;
using UIKit;

namespace Nkraft.Mopups.Platforms.iOS;

internal sealed class PopupWindow : UIWindow
{
    private PopupPage? _page;
    private double _lastBackgroundTouchTimestamp = -1;

    public PopupWindow(UIWindowScene scene, PopupPage page, UIWindow? hostWindow) : base(scene)
    {
        _page = page;
        HostWindow = hostWindow;
    }

    /// <summary>The app window this popup sits on top of; gets key status back when the last popup closes.</summary>
    public UIWindow? HostWindow { get; private set; }

    public override UIView? HitTest(CGPoint point, UIEvent? uievent)
    {
        var hit = base.HitTest(point, uievent);
        var page = _page;

        if (hit is null || page is null)
            return hit;

        // Returning null lets the touch fall through to the windows below.
        if (page.InputTransparent)
            return null;

        if (page.BackgroundInputTransparent && IsBackground(hit))
        {
            // UIKit may hit-test several times for a single touch, and also for pointer hover on iPad.
            // Report the background tap once per touch event.
            if (uievent is { Type: UIEventType.Touches } && uievent.Timestamp != _lastBackgroundTouchTimestamp)
            {
                _lastBackgroundTouchTimestamp = uievent.Timestamp;
                page.SendBackgroundClick();
            }

            return null;
        }

        return hit;
    }

    private bool IsBackground(UIView hit) =>
        ReferenceEquals(hit, this)
        || ReferenceEquals(hit, RootViewController?.View)
        || (RootViewController is PopupRootViewController root && root.IsBackgroundView(hit));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _page = null;
            HostWindow = null;
        }

        base.Dispose(disposing);
    }
}