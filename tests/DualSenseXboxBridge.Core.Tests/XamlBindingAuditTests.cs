using System.Xml.Linq;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

/// <summary>
/// Static guard for a WPF trap: RangeBase.Value, ToggleButton.IsChecked and TextBox.Text default
/// to a TwoWay binding. Binding those to a read-only view-model property throws
/// "A TwoWay or OneWayToSource binding cannot work on the read-only property 'X'", so every such
/// binding must state its Mode explicitly (telemetry uses OneWay).
/// </summary>
public class XamlBindingAuditTests
{
    private static readonly Dictionary<string, string[]> TwoWayByDefaultTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProgressBar"] = new[] { "Value" },
        ["Slider"] = new[] { "Value" },
        ["CheckBox"] = new[] { "IsChecked" },
        ["TextBox"] = new[] { "Text" },
        ["ComboBox"] = new[] { "Text", "SelectedValue", "SelectedItem" },
        ["DatePicker"] = new[] { "Text", "SelectedDate" }
    };

    [Fact]
    public void BindingsOnTwoWayTargets_DeclareTheirModeExplicitly()
    {
        var document = XDocument.Load(MainWindowXamlPath());
        var offenders = new List<string>();

        foreach (var element in document.Descendants())
        {
            if (!TwoWayByDefaultTargets.TryGetValue(element.Name.LocalName, out var twoWayProperties))
            {
                continue;
            }

            foreach (var attribute in element.Attributes())
            {
                if (!twoWayProperties.Contains(attribute.Name.LocalName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = attribute.Value.Trim();
                if (!value.StartsWith("{Binding", StringComparison.Ordinal))
                {
                    continue;
                }

                if (value.IndexOf("Mode=", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    offenders.Add($"{element.Name.LocalName}.{attribute.Name.LocalName} -> {value}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These bindings target properties that default to TwoWay and must declare Mode explicitly "
            + "(use Mode=OneWay for read-only telemetry):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders));
    }

    [Theory]
    [InlineData("TriggerLt")]
    [InlineData("TriggerRt")]
    public void TriggerTelemetry_IsBoundOneWay(string propertyName)
    {
        var xaml = File.ReadAllText(MainWindowXamlPath());

        Assert.Contains($"Value=\"{{Binding {propertyName}, Mode=OneWay}}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryReadOnlyTelemetryProgressBar_UsesOneWay()
    {
        var document = XDocument.Load(MainWindowXamlPath());

        var progressBarBindings = document
            .Descendants()
            .Where(e => e.Name.LocalName == "ProgressBar")
            .Select(e => e.Attribute("Value")?.Value)
            .Where(v => v is not null && v.StartsWith("{Binding", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(progressBarBindings);
        Assert.All(progressBarBindings, binding => Assert.Contains("Mode=OneWay", binding, StringComparison.Ordinal));
    }

    private static string MainWindowXamlPath()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "src", "DualSenseXboxBridge", "MainWindow.xaml");
        Assert.True(File.Exists(path), $"MainWindow.xaml was not found at {path}");
        return path;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DualSenseXboxBridge.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root (DualSenseXboxBridge.sln) was not found.");
    }
}
