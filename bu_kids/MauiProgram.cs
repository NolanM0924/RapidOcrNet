using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace bu_kids;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Configure file system permissions and other essential services
		builder.ConfigureEssentials(essentials =>
		{
			// Add any essential configurations here if needed
		});

		return builder.Build();
	}
}
