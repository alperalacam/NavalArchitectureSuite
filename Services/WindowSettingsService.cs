using System;
using System.IO;
using System.Text.Json;

namespace NavalArchitectureSuite.Services
{
    public class WindowSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Maximized { get; set; }
    }

    public static class WindowSettingsService
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NavalArchitectureSuite", "window.json");

        public static WindowSettings? Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<WindowSettings>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void Save(WindowSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(settings));
            }
            catch
            {
                // Persisting window bounds is best-effort; ignore failures (e.g. read-only profile).
            }
        }
    }
}
