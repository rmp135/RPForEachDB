using System;
using System.Text.Json;

namespace RPForEachDB;

public interface IConfigurationManager
{
    Configuration Configuration { get; }
    void Save();
}
public class ConfigurationManager: IConfigurationManager
{
    public Configuration Configuration { get; }
    public ConfigurationManager()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var fullPath = System.IO.Path.Combine(path, "RPForEachDB.json");
        if (System.IO.File.Exists(fullPath))
        {
            var json = System.IO.File.ReadAllText(fullPath);
            Configuration = JsonSerializer.Deserialize<Configuration>(json);
        }
        else
        {
            Configuration = new Configuration();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Configuration);
        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var fullPath = System.IO.Path.Combine(path, "RPForEachDB.json");
        System.IO.File.WriteAllText(fullPath, json);
    }
}