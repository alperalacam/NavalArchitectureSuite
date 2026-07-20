using System;
using System.IO;
using System.Text.Json;

namespace NavalArchitectureSuite.Services
{
    public static class HullViewportSettingsService
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NavalArchitectureSuite", "hull_viewport.json");

        public static WindowSettings? Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<WindowSettings>(json);
                // Sanity check — ignore if values are non-physical.
                return (s is { Width: > 200, Height: > 200 }) ? s : null;
            }
            catch { return null; }
        }

        public static void Save(WindowSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(settings));
            }
            catch { }
        }
    }
}
