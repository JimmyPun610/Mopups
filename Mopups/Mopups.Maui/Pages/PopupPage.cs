using System.Windows.Input;
using AsyncAwaitBestPractices;
using Mopups.Animations;
using Mopups.Animations.Base;
using Mopups.Enums;
using Mopups.Services;

namespace Mopups.Pages;

public class PopupPage : ContentPage
{
    public event EventHandler? BackgroundClicked;

    internal Task? AppearingTransactionTask { get; set; }

    internal Task? DisappearingTransactionTask { get; set; }

    public static readonly BindableProperty IsAnimationEnabledProperty = BindableProperty.Create(nameof(IsAnimationEnabled), typeof(bool), typeof(PopupPage), true);

    public bool IsAnimationEnabled
    {
        get => (bool)GetValue(IsAnimationEnabledProperty) && AnimationHelper.SystemAnimationsEnabled;
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>
    /// When true (default), <see cref="ContentPage.Padding"/> is owned by Mopups and set from
    /// <see cref="SystemPadding"/>. Put your own spacing on the content's Margin instead.
    /// </summary>
    public static readonly BindableProperty HasSystemPaddingProperty = BindableProperty.Create(nameof(HasSystemPadding), typeof(bool), typeof(PopupPage), true);
    public bool HasSystemPadding
    {
        get => (bool)GetValue(HasSystemPaddingProperty);
        set => SetValue(HasSystemPaddingProperty, value);
    }

    public static readonly BindableProperty AnimationProperty = BindableProperty.Create(nameof(Animation), typeof(IPopupAnimation), typeof(PopupPage), new ScaleAnimation());
    public IPopupAnimation? Animation
    {
        get => (IPopupAnimation?)GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    public static readonly BindableProperty SystemPaddingProperty = BindableProperty.Create(nameof(SystemPadding), typeof(Thickness), typeof(PopupPage), default(Thickness), BindingMode.OneWayToSource);
    public Thickness SystemPadding
    {
        get => (Thickness)GetValue(SystemPaddingProperty);
        internal set => SetValue(SystemPaddingProperty, value);
    }

    public static readonly BindableProperty SystemPaddingSidesProperty = BindableProperty.Create(nameof(SystemPaddingSides), typeof(PaddingSide), typeof(PopupPage), PaddingSide.All);
    public PaddingSide SystemPaddingSides
    {
        get => (PaddingSide)GetValue(SystemPaddingSidesProperty);
        set => SetValue(SystemPaddingSidesProperty, value);
    }

    public static readonly BindableProperty CloseWhenBackgroundIsClickedProperty = BindableProperty.Create(nameof(CloseWhenBackgroundIsClicked), typeof(bool), typeof(PopupPage), true);
    public bool CloseWhenBackgroundIsClicked
    {
        get => (bool)GetValue(CloseWhenBackgroundIsClickedProperty);
        set => SetValue(CloseWhenBackgroundIsClickedProperty, value);
    }

    public static readonly BindableProperty BackgroundInputTransparentProperty = BindableProperty.Create(nameof(BackgroundInputTransparent), typeof(bool), typeof(PopupPage), false);
    public bool BackgroundInputTransparent
    {
        get => (bool)GetValue(BackgroundInputTransparentProperty);
        set => SetValue(BackgroundInputTransparentProperty, value);
    }

    /// <summary>
    /// When true (default), the bottom padding grows to at least <see cref="KeyboardOffset"/>
    /// so the content stays above the soft keyboard.
    /// </summary>
    public static readonly BindableProperty HasKeyboardOffsetProperty = BindableProperty.Create(nameof(HasKeyboardOffset), typeof(bool), typeof(PopupPage), true);
    public bool HasKeyboardOffset
    {
        get => (bool)GetValue(HasKeyboardOffsetProperty);
        set => SetValue(HasKeyboardOffsetProperty, value);
    }

    public static readonly BindableProperty KeyboardOffsetProperty = BindableProperty.Create(nameof(KeyboardOffset), typeof(double), typeof(PopupPage), 0d, BindingMode.OneWayToSource);
    public double KeyboardOffset
    {
        get => (double)GetValue(KeyboardOffsetProperty);
        private set => SetValue(KeyboardOffsetProperty, value);
    }

    public static readonly BindableProperty BackgroundClickedCommandProperty = BindableProperty.Create(nameof(BackgroundClickedCommand), typeof(ICommand), typeof(PopupPage));
    public ICommand BackgroundClickedCommand
    {
        get => (ICommand)GetValue(BackgroundClickedCommandProperty);
        set => SetValue(BackgroundClickedCommandProperty, value);
    }

    public static readonly BindableProperty BackgroundClickedCommandParameterProperty = BindableProperty.Create(nameof(BackgroundClickedCommandParameter), typeof(object), typeof(PopupPage));
    public object BackgroundClickedCommandParameter
    {
        get => GetValue(BackgroundClickedCommandParameterProperty);
        init => SetValue(BackgroundClickedCommandParameterProperty, value);
    }

    public static readonly BindableProperty DisableAndroidAccessibilityHandlingProperty = BindableProperty.Create(nameof(DisableAndroidAccessibilityHandling), typeof(bool), typeof(PopupPage), false);
    public bool DisableAndroidAccessibilityHandling
    {
        get => (bool)GetValue(DisableAndroidAccessibilityHandlingProperty);
        init => SetValue(DisableAndroidAccessibilityHandlingProperty, value);
    }

    public PopupPage()
    {
        BackgroundColor = Colors.Transparent;
    }

    protected override bool OnBackButtonPressed() => false;

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(HasSystemPadding):
            case nameof(HasKeyboardOffset):
            case nameof(SystemPaddingSides):
            case nameof(SystemPadding):
            case nameof(KeyboardOffset):
                ApplySystemPadding();
                break;
        }
    }

    /// <summary>
    /// Replaces the old one-shot "apply padding after AddAsync" hack in PopupNavigation and the
    /// commented-out LayoutChildren override. Runs every time the platform reports new insets,
    /// so rotation and keyboard show/hide are handled on both platforms.
    /// </summary>
    private void ApplySystemPadding()
    {
        if (!HasSystemPadding && !HasKeyboardOffset)
            return;

        var system = HasSystemPadding ? SystemPadding : default;
        var sides = SystemPaddingSides;

        var left = sides.HasFlag(PaddingSide.Left) ? system.Left : 0;
        var top = sides.HasFlag(PaddingSide.Top) ? system.Top : 0;
        var right = sides.HasFlag(PaddingSide.Right) ? system.Right : 0;
        var bottom = sides.HasFlag(PaddingSide.Bottom) ? system.Bottom : 0;

        if (HasKeyboardOffset)
            bottom = Math.Max(bottom, KeyboardOffset);

        // Thickness has value equality, so an unchanged value doesn't trigger another layout pass.
        Padding = new Thickness(left, top, right, bottom);
    }

    #region Animation Methods

    internal void PreparingAnimation()
    {
        if (IsAnimationEnabled)
            Animation?.Preparing(Content, this);
    }

    internal void DisposingAnimation()
    {
        if (IsAnimationEnabled)
            Animation?.Disposing(Content, this);
    }

    internal async Task AppearingAnimation()
    {
        OnAppearingAnimationBegin();
        await OnAppearingAnimationBeginAsync();

        if (IsAnimationEnabled && Animation != null)
            await Animation.Appearing(Content, this);

        OnAppearingAnimationEnd();
        await OnAppearingAnimationEndAsync();
    }

    internal async Task DisappearingAnimation()
    {
        OnDisappearingAnimationBegin();
        await OnDisappearingAnimationBeginAsync();

        if (IsAnimationEnabled && Animation != null)
            await Animation.Disappearing(Content, this);

        OnDisappearingAnimationEnd();
        await OnDisappearingAnimationEndAsync();
    }

    #endregion

    #region Override Animation Methods

    protected virtual void OnAppearingAnimationBegin()
    {
    }

    protected virtual void OnAppearingAnimationEnd()
    {
    }

    protected virtual void OnDisappearingAnimationBegin()
    {
    }

    protected virtual void OnDisappearingAnimationEnd()
    {
    }

    protected virtual Task OnAppearingAnimationBeginAsync() => Task.CompletedTask;

    protected virtual Task OnAppearingAnimationEndAsync() => Task.CompletedTask;

    protected virtual Task OnDisappearingAnimationBeginAsync() => Task.CompletedTask;

    protected virtual Task OnDisappearingAnimationEndAsync() => Task.CompletedTask;

    #endregion

    protected virtual bool OnBackgroundClicked()
    {
        return CloseWhenBackgroundIsClicked;
    }

    internal bool SendBackgroundClick()
    {
        BackgroundClicked?.Invoke(this, EventArgs.Empty);
        if (BackgroundClickedCommand?.CanExecute(BackgroundClickedCommandParameter) == true)
        {
            BackgroundClickedCommand.Execute(BackgroundClickedCommandParameter);
        }
        if (OnBackgroundClicked())
        {
            MopupService.Instance.RemovePageAsync(this).SafeFireAndForget();
            return true;
        }
        return false;
    }
}