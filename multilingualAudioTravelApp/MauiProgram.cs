<<<<<<< HEAD
﻿using SkiaSharp.Views.Maui.Controls.Hosting;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
=======
﻿using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ZXing.Net.Maui.Controls;
>>>>>>> ff8a97278a0ea02e6f9fca984ac685ded32a0813

namespace multilingualAudioTravelApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
<<<<<<< HEAD
                .UseLocalNotification()
=======
                .UseBarcodeReader()
>>>>>>> ff8a97278a0ea02e6f9fca984ac685ded32a0813
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}