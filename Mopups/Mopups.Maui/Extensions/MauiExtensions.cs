namespace Mopups.Extensions;

internal static class MauiExtensions
{
    extension(Element element)
    {
        public IMauiContext? FindMauiContext(bool fallbackToAppMauiContext = true)
        {
            if (element is IElement { Handler.MauiContext: not null } fe)
                return fe.Handler.MauiContext;

            foreach (var parent in element.GetParentsPath())
            {
                if (parent is IElement { Handler.MauiContext: not null } parentView)
                    return parentView.Handler.MauiContext;
            }

            return fallbackToAppMauiContext ? Application.Current?.FindMauiContext() : null;
        }

        public IEnumerable<Element> GetParentsPath()
        {
            var current = element;

            while (IsApplicationOrNull(current.RealParent) == false)
            {
                current = current.RealParent;
                yield return current;
            }
        }
    }

    public static bool IsApplicationOrNull(object? element) =>
        element is null or IApplication;
}