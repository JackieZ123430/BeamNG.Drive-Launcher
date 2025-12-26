using System;
using System.Collections.Generic;
using System.IO;

namespace BeamNGLauncher
{
    public class LocalizationService
    {
        private const string LanguageFolderName = "Languages";
        private const string PreferenceFileName = "language.pref";
        private readonly Dictionary<string, Dictionary<string, string>> _translations =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public string CurrentLanguage { get; private set; } = "zh-CN";

        public void LoadAvailableLanguages(string baseDirectory)
        {
            _translations.Clear();

            if (string.IsNullOrWhiteSpace(baseDirectory))
                return;

            string langDir = Path.Combine(baseDirectory, LanguageFolderName);
            if (!Directory.Exists(langDir))
                return;

            foreach (var file in Directory.GetFiles(langDir, "*.lang", SearchOption.TopDirectoryOnly))
            {
                var langCode = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(langCode))
                    continue;

                var map = LoadLanguageFile(file);
                if (map.Count > 0)
                    _translations[langCode] = map;
            }
        }

        public void SetCurrentLanguage(string languageCode)
        {
            if (!string.IsNullOrWhiteSpace(languageCode))
                CurrentLanguage = languageCode;
        }

        public string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            if (_translations.TryGetValue(CurrentLanguage, out var map) && map.TryGetValue(key, out var value))
                return value;
            if (_translations.TryGetValue("zh-CN", out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;
            return key;
        }

        public string LoadLanguagePreference()
        {
            string prefPath = GetPreferencePath();
            if (!File.Exists(prefPath))
                return null;

            try
            {
                return File.ReadAllText(prefPath).Trim();
            }
            catch
            {
                return null;
            }
        }

        public void SaveLanguagePreference(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return;

            string prefPath = GetPreferencePath();
            string prefDir = Path.GetDirectoryName(prefPath);
            if (!string.IsNullOrWhiteSpace(prefDir))
                Directory.CreateDirectory(prefDir);

            File.WriteAllText(prefPath, languageCode);
        }

        private Dictionary<string, string> LoadLanguageFile(string filePath)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var raw in File.ReadAllLines(filePath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;

                    int idx = line.IndexOf('=');
                    if (idx <= 0)
                        continue;

                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();

                    if (key.Length == 0)
                        continue;

                    map[key] = value;
                }
            }
            catch
            {
            }

            return map;
        }

        private string GetPreferencePath()
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "BeamNGLauncher", PreferenceFileName);
        }
    }
}
