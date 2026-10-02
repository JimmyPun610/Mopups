using Mopups.Pages;

namespace Mopups.Animations.Base;

public abstract class FadeBackgroundAnimation : BaseAnimation
{
    private Color? _backgroundColor;

    public bool HasBackgroundAnimation { get; set; } = true;

    public override void Preparing(View? content, PopupPage page)
    {
        _backgroundColor = page.BackgroundColor;

        page.BackgroundColor = GetColor(0);
    }

    public override void Disposing(View? content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            page.BackgroundColor = _backgroundColor;
        }
    }

    public override Task Appearing(View? content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            TaskCompletionSource<bool> taskSource = new();

            page.Animate(name: "backgroundFade", callback: Callback, start: 0, end: _backgroundColor?.Alpha ?? 0, length: DurationIn, finished: FinishedAnimation);

            return taskSource.Task;

            void Callback(double d) => page.BackgroundColor = GetColor(d);

            void FinishedAnimation(double d, bool b) => taskSource.SetResult(true);
        }

        return Task.CompletedTask;
    }

    public override Task Disappearing(View? content, PopupPage page)
    {
        if (HasBackgroundAnimation)
        {
            TaskCompletionSource<bool> taskSource = new();

            _backgroundColor = page.BackgroundColor;

            page.Animate("backgroundFade", Callback, _backgroundColor.Alpha, 0, length: DurationOut, finished: FinishedAnimation);

            return taskSource.Task;

            void Callback(double d) => page.BackgroundColor = GetColor(d);
            void FinishedAnimation(double d, bool b) => taskSource.SetResult(true);
        }

        return Task.CompletedTask;
    }

    private Color GetColor(double transparent)
    {
        return new Color(_backgroundColor?.Red ?? 255f, _backgroundColor?.Green ?? 255f, _backgroundColor?.Blue ?? 255f, (float)transparent);
    }
}
