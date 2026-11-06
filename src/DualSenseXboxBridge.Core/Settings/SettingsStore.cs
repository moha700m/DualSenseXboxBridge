using System.Text.Json;
using System.Text.Json.Serialization;

namespace DualSenseXboxBridge.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON. Writes are atomic (temp file + move)
/// so a crash during save cannot leave a truncated settings file behind.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public SettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? DefaultPath;
    }

    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DualSenseXboxBridge");

    public static string DefaultPath => Path.Combine(DefaultDirectory, "settings.json");

    public string FilePath => _filePath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return AppSettings.CreateDefault();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
            if (settings is null)
            {
                return AppSettings.CreateDefault();
            }

            settings.Normalize();
            return settings;
        }
        catch (Exception)
        {
            // A corrupt file must never stop the app from starting; keep a copy for support.
            TryBackupCorruptFile();
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        var temporary = _filePath + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, _filePath, overwrite: true);
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Copy(_filePath, _filePath + ".corrupt", overwrite: true);
            }
        }
        catch
        {
            // Best effort only.
        }
    }
}
