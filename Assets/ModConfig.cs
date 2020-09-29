using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using UnityEngine;

sealed class ModConfig<T> where T : new()
{
    private readonly string _settingsPath = null;
    private static readonly object _settingsFileLock = new object();

    public ModConfig(string filename)
    {
        _settingsPath = Application.isEditor ? @"Assets\ZooSettings.json" : Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), filename + ".json");
    }

    public string SerializeSettings(T settings)
    {
        return JsonConvert.SerializeObject(settings, Formatting.Indented, new StringEnumConverter());
    }

    public T ReadSettings()
    {
        try
        {
            lock (_settingsFileLock)
            {
                if (!File.Exists(_settingsPath))
                    File.WriteAllText(_settingsPath, SerializeSettings(new T()));

                var deserialized = JsonConvert.DeserializeObject<T>(
                    File.ReadAllText(_settingsPath),
                    new JsonSerializerSettings { Error = (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) => args.ErrorContext.Handled = true });
                return deserialized != null ? deserialized : new T();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return new T();
        }
    }

    public void WriteSettings(T value)
    {
        lock (_settingsFileLock)
            File.WriteAllText(_settingsPath, SerializeSettings(value));
    }

    public override string ToString()
    {
        return SerializeSettings(ReadSettings());
    }
}
