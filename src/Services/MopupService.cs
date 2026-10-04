using Nkraft.Mopups.Contracts;

namespace Nkraft.Mopups.Services;

public static class MopupService
{
    private static readonly Lazy<IPopupNavigation> Implementation = new(CreatePopupNavigation, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Gets if the plugin is supported on the current platform.
    /// </summary>
    public static bool IsSupported => Implementation.Value != null;

    /// <summary>
    /// Current plugin implementation to use
    /// </summary>
    public static IPopupNavigation Instance
    {
        get
        {
            var lazyEvalPopupNavigation = Implementation.Value;

            return lazyEvalPopupNavigation ?? throw NotImplementedInReferenceAssembly();
        }
    }

    private static PopupNavigation CreatePopupNavigation() => new PopupNavigation();

    internal static Exception NotImplementedInReferenceAssembly() =>
        new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
}


