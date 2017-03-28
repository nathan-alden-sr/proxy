using System;
using System.IO;
using System.Text;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace NathanAlden.Proxy.Services.ConfigService
{
    public class ConfigService : IConfigService
    {
        private readonly Lazy<ConfigModel> _config = new Lazy<ConfigModel>(
            () =>
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.yml");

                if (File.Exists(configPath))
                {
                    Deserializer deserializer = new DeserializerBuilder()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .WithNodeDeserializer(x => new ValidatingNodeDeserializer(x), x => x.InsteadOf<ObjectNodeDeserializer>())
                        .Build();
                    string yaml = File.ReadAllText(configPath, Encoding.UTF8);

                    Log.Information($"Using config file {configPath}");

                    try
                    {
                        return deserializer.Deserialize<ConfigModel>(yaml);
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception, "Error parsing config file; using defaults");

                        return new ConfigModel();
                    }
                }

                Log.Information($"Config file '{configPath}' not found; using defaults");

                return new ConfigModel();
            });

        public ConfigModel Config => _config.Value;
    }
}