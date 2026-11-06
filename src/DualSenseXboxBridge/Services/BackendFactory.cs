using System.Net.Sockets;
using DualSenseXboxBridge.Backends;
using DualSenseXboxBridge.Core.Backends;
using DualSenseXboxBridge.Core.Diagnostics;
using DualSenseXboxBridge.Core.Settings;

namespace DualSenseXboxBridge.Services;

public static class BackendFactory
{
    public static IVirtualPadBackend Create(AppSettings settings, bool forceNull = false)
    {
        if (forceNull || settings.Backend == BackendPreference.Null)
        {
            return new NullPadBackend();
        }

        if (settings.Backend == BackendPreference.Viiper)
        {
            return CreateViiper(settings);
        }

        if (settings.Backend == BackendPreference.ViGEm)
        {
            return new ViGEmBackend();
        }

        // Auto: VIIPER first (maintained), ViGEmBus only when it is already installed.
        if (ViiperServerLocator.Find() is not null || IsPortOpen(settings.ViiperHost, settings.ViiperPort))
        {
            return CreateViiper(settings);
        }

        if (ViGEmProbe.IsDriverInstalled())
        {
            AppLog.Info("Backend auto-selection: ViGEmBus (VIIPER not found).");
            return new ViGEmBackend();
        }

        AppLog.Info("Backend auto-selection: VIIPER default (no driver detected yet).");
        return CreateViiper(settings);
    }

    private static ViiperBackend CreateViiper(AppSettings settings) =>
        new(settings.ViiperHost, settings.ViiperPort, settings.ViiperServerPath, settings.ViiperAutoLaunch);

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(host, port);
            if (!task.Wait(TimeSpan.FromMilliseconds(250)))
            {
                return false;
            }

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
