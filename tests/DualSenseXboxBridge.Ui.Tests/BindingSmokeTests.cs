using System.Diagnostics;
using System.Threading;
using Xunit;

namespace DualSenseXboxBridge.Ui.Tests;

/// <summary>
/// Loads the real WPF window on an STA thread and fails if WPF reports any data-binding error.
/// This is the runtime counterpart to the static XAML audit: it catches the
/// "TwoWay binding cannot work on a read-only property" family of errors before shipping.
/// </summary>
public class BindingSmokeTests
{
    [Fact]
    public void MainWindow_LoadsWithoutBindingErrors()
    {
        var bindingErrors = new List<string>();
        Exception? failure = null;

        var uiThread = new Thread(() =>
        {
            var listener = new BindingErrorListener(bindingErrors);
            try
            {
                PresentationTraceSources.Refresh();
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

                var application = new App();
                application.InitializeComponent();

                var window = new MainWindow();
                GC.KeepAlive(window);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
            }
        });

        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.IsBackground = true;
        uiThread.Start();

        Assert.True(uiThread.Join(TimeSpan.FromSeconds(90)), "The WPF smoke test timed out.");

        if (failure is not null)
        {
            throw new Xunit.Sdk.XunitException(
                "Creating MainWindow threw: " + failure + Environment.NewLine + string.Join(Environment.NewLine, bindingErrors));
        }

        Assert.True(
            bindingErrors.Count == 0,
            "WPF reported data-binding errors while loading MainWindow:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, bindingErrors));
    }

    private sealed class BindingErrorListener : TraceListener
    {
        private readonly List<string> _errors;

        public BindingErrorListener(List<string> errors) => _errors = errors;

        public override void Write(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                lock (_errors)
                {
                    _errors.Add(message!);
                }
            }
        }

        public override void WriteLine(string? message) => Write(message);
    }
}
