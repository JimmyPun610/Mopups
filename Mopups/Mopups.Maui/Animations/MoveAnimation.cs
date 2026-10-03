
using System.ComponentModel;
using Nkraft.Mopups.Animations.Base;
using Nkraft.Mopups.Enums;
using Nkraft.Mopups.Pages;

namespace Nkraft.Mopups.Animations;

public class MoveAnimation : FadeBackgroundAnimation
{
    private double _defaultTranslationX;
    private double _defaultTranslationY;

    public MoveAnimationOptions PositionIn { get; set; }
    public MoveAnimationOptions PositionOut { get; set; }

    public MoveAnimation() : this(MoveAnimationOptions.Bottom, MoveAnimationOptions.Bottom) { }

    public MoveAnimation(MoveAnimationOptions positionIn, MoveAnimationOptions positionOut)
    {
        PositionIn = positionIn;
        PositionOut = positionOut;

        DurationIn = DurationOut = 300;
        EasingIn = Easing.SinOut;
        EasingOut = Easing.SinIn;
    }

    public override void Preparing(View? content, PopupPage page)
    {
        base.Preparing(content, page);

        HidePage(page);

        if (content is not null)
        {
            UpdateDefaultTranslations(content);
        }
    }

    public override void Disposing(View? content, PopupPage page)
    {
        base.Disposing(content, page);

        ShowPage(page);

        if (content is not null)
        {
            content.TranslationX = _defaultTranslationX;
            content.TranslationY = _defaultTranslationY;
        }
    }

    public override Task Appearing(View? content, PopupPage page)
    {
        var taskList = new List<Task>
        {
            base.Appearing(content, page)
        };

        if (content is not null)
        {
            var topOffset = GetTopOffset(content, page);
            var leftOffset = GetLeftOffset(content, page);

            switch (PositionIn)
            {
                case MoveAnimationOptions.Top:
                    content.TranslationY = -topOffset;
                    break;
                case MoveAnimationOptions.Bottom:
                    content.TranslationY = topOffset;
                    break;
                case MoveAnimationOptions.Left:
                    content.TranslationX = -leftOffset;
                    break;
                case MoveAnimationOptions.Right:
                    content.TranslationX = leftOffset;
                    break;
                case MoveAnimationOptions.Center:
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(PositionIn), (int)PositionIn,
                        typeof(MoveAnimationOptions));
            }

            content.HeightRequest = content.Height;
            content.WidthRequest = content.Width;

            taskList.Add(content.TranslateToAsync(_defaultTranslationX, _defaultTranslationY, DurationIn, EasingIn));
        }

        ShowPage(page);

        return Task.WhenAll(taskList);
    }

    public override Task Disappearing(View? content, PopupPage page)
    {
        var taskList = new List<Task>
        {
            base.Disappearing(content, page)
        };
        
        if (content is null)
            return Task.WhenAll(taskList);

        UpdateDefaultTranslations(content);

        var topOffset = GetTopOffset(content, page);
        var leftOffset = GetLeftOffset(content, page);

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

        return Task.WhenAll(taskList);
    }

    private void UpdateDefaultTranslations(View content)
    {
        _defaultTranslationX = content.TranslationX;
        _defaultTranslationY = content.TranslationY;
    }
}
