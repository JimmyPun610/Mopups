
using System.ComponentModel;
using Nkraft.Mopups.Enums;
using Nkraft.Mopups.Pages;

namespace Nkraft.Mopups.Animations;

public class ScaleAnimation : FadeAnimation
{
    private double _defaultScale;
    private double _defaultOpacity;
    private double _defaultTranslationX;
    private double _defaultTranslationY;

    public double ScaleIn { get; set; } = 0.8;
    public double ScaleOut { get; set; } = 0.8;

    public MoveAnimationOptions PositionIn { get; set; }
    public MoveAnimationOptions PositionOut { get; set; }

    public ScaleAnimation() : this(MoveAnimationOptions.Center, MoveAnimationOptions.Center) { }

    public ScaleAnimation(MoveAnimationOptions positionIn, MoveAnimationOptions positionOut)
    {
        PositionIn = positionIn;
        PositionOut = positionOut;
        EasingIn = Easing.SinOut;
        EasingOut = Easing.SinIn;

        if (PositionIn != MoveAnimationOptions.Center) DurationIn = 500;
        if (PositionOut != MoveAnimationOptions.Center) DurationOut = 500;
    }

    public override void Preparing(View? content, PopupPage page)
    {
        if (HasBackgroundAnimation) base.Preparing(content, page);

        HidePage(page);

        if (content == null) return;

        UpdateDefaultProperties(content);

        if (!HasBackgroundAnimation) content.Opacity = 0;
    }

    public override void Disposing(View? content, PopupPage page)
    {
        if (HasBackgroundAnimation) base.Disposing(content, page);

        ShowPage(page);

        if (content is null) 
            return;

        content.Scale = _defaultScale;
        content.Opacity = _defaultOpacity;
        content.TranslationX = _defaultTranslationX;
        content.TranslationY = _defaultTranslationY;
    }

    public override async Task Appearing(View? content, PopupPage page)
    {
        var taskList = new List<Task> { base.Appearing(content, page) };

        if (content is not null)
        {
            var topOffset = GetTopOffset(content, page) * ScaleIn;
            var leftOffset = GetLeftOffset(content, page) * ScaleIn;

            taskList.Add(Scale(content, EasingIn, ScaleIn, _defaultScale, true));

            switch (PositionIn)
            {
                case MoveAnimationOptions.Top:
                    content.TranslationY = -topOffset;
                    taskList.Add(content.TranslateToAsync(_defaultTranslationX, _defaultTranslationY, DurationIn, EasingIn));
                    break;
                case MoveAnimationOptions.Bottom:
                    content.TranslationY = topOffset;
                    taskList.Add(content.TranslateToAsync(_defaultTranslationX, _defaultTranslationY, DurationIn, EasingIn));
                    break;
                case MoveAnimationOptions.Left:
                    content.TranslationX = -leftOffset;
                    taskList.Add(content.TranslateToAsync(_defaultTranslationX, _defaultTranslationY, DurationIn, EasingIn));
                    break;
                case MoveAnimationOptions.Right:
                    content.TranslationX = leftOffset;
                    taskList.Add(content.TranslateToAsync(_defaultTranslationX, _defaultTranslationY, DurationIn, EasingIn));
                    break;
                case MoveAnimationOptions.Center:
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(PositionIn), (int)PositionIn, typeof(MoveAnimationOptions));
            }
        }

        ShowPage(page);

        await Task.WhenAll(taskList);
    }

    public override async Task Disappearing(View? content, PopupPage page)
    {
        var taskList = new List<Task> { base.Disappearing(content, page) };

        if (content is not null)
        {
            UpdateDefaultProperties(content);

            var topOffset = GetTopOffset(content, page) * ScaleOut;
            var leftOffset = GetLeftOffset(content, page) * ScaleOut;

            taskList.Add(Scale(content, EasingOut, _defaultScale, ScaleOut, false));

            switch (PositionOut)
            {
                case MoveAnimationOptions.Top:
                    taskList.Add(content.TranslateToAsync(_defaultTranslationX, -topOffset, DurationOut, EasingOut));
                    break;
                case MoveAnimationOptions.Bottom:
                    taskList.Add(content.TranslateToAsync(_defaultTranslationX, topOffset, DurationOut, EasingOut));
                    break;
                case MoveAnimationOptions.Left:
                    taskList.Add(content.TranslateToAsync(-leftOffset, _defaultTranslationY, DurationOut, EasingOut));
                    break;
                case MoveAnimationOptions.Right:
                    taskList.Add(content.TranslateToAsync(leftOffset, _defaultTranslationY, DurationOut, EasingOut));
                    break;
                case MoveAnimationOptions.Center:
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(PositionOut), (int)PositionOut, typeof(MoveAnimationOptions));
            }
        }

        await Task.WhenAll(taskList);
    }

    private Task<bool> Scale(View content, Easing easing, double start, double end, bool isAppearing)
    {
        var task = new TaskCompletionSource<bool>();

        content.Animate("popIn", d =>
        {
            content.Scale = double.IsNaN(d) ? 1 : d;
        }, start, end,
        easing: easing,
        length: isAppearing ? DurationIn : DurationOut,
        finished: (d, b) =>
        {
            task.SetResult(true);
        });

        return task.Task;
    }

    private void UpdateDefaultProperties(View content)
    {
        _defaultScale = content.Scale;
        _defaultOpacity = content.Opacity;

        if (double.IsNaN(_defaultOpacity))
            _defaultOpacity = 1;

        _defaultTranslationX = content.TranslationX;
        _defaultTranslationY = content.TranslationY;
    }
}
