using System.Windows;
using System.Windows.Threading;
using DualSenseXboxBridge.Core.Diagnostics;

namespace DualSenseXboxBridge;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Error("Unhandled UI exception", e.Exception);
        MessageBox.Show(
            "حدث خطأ غير متوقع. تم تسجيل التفاصيل في مجلد السجلات.\n\n" + e.Exception.Message,
            "DualSense Xbox Bridge",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            AppLog.Error("Unhandled background exception", exception);
        }
    }
}
