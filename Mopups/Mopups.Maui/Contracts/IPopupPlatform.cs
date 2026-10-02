using Mopups.Pages;

namespace Mopups.Contracts;

public interface IPopupPlatform
{
    Task AddAsync(PopupPage page);

    Task RemoveAsync(PopupPage page);

    public static IViewHandler GetOrCreateHandler<TPopupPageHandler>(VisualElement bindable) where TPopupPageHandler : IViewHandler, new()
    {
        return bindable.Handler ??= new TPopupPageHandler();
    }
}
