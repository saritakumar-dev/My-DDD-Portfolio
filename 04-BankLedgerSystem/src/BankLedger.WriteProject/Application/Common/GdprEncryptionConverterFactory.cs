using BankLedger.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BankLedger.WriteProject.Application.Common
{
    public class GdprEncryptionConverterFactory : JsonConverterFactory
    {
        private readonly ICryptoKeyVault _keyVault;
        public GdprEncryptionConverterFactory(ICryptoKeyVault keyVault) => _keyVault = keyVault;
        public override bool CanConvert(Type typeToConvert) => true;
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new GdprEventConverter(_keyVault, typeToConvert);
    }

    public class GdprEventConverter : JsonConverter<object>
    {
        private readonly ICryptoKeyVault _keyVault;
        private readonly Type _eventType;

        public GdprEventConverter(ICryptoKeyVault keyVault, Type eventType) { _keyVault = keyVault; _eventType = eventType; }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var optionsCopy = new JsonSerializerOptions(options);
            for (int i = optionsCopy.Converters.Count - 1; i >= 0; i--)
                if (optionsCopy.Converters[i] is GdprEncryptionConverterFactory) optionsCopy.Converters.RemoveAt(i);

            var instance = JsonSerializer.Deserialize(ref reader, _eventType, optionsCopy);
            if (instance == null) return null;

            Guid aggregateId = AmbientContext.CurrentAggregateId;
            foreach (var prop in _eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType == typeof(string) && prop.IsDefined(typeof(GdprEncryptedAttribute), true))
                {
                    var cipherText = prop.GetValue(instance) as string;
                    if (string.IsNullOrEmpty(cipherText)) continue;
                    try
                    {
                        string key = _keyVault.GetOrCreateKeyAsync(aggregateId).GetAwaiter().GetResult();
                        prop.SetValue(instance, CryptoEngine.Decrypt(cipherText, key));
                    }
                    catch { prop.SetValue(instance, "[DATA_ERASED_UNDER_GDPR]"); }
                }
            }
            return instance;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            Guid aggregateId = AmbientContext.CurrentAggregateId;
            var optionsCopy = new JsonSerializerOptions(options);
            for (int i = optionsCopy.Converters.Count - 1; i >= 0; i--)
                if (optionsCopy.Converters[i] is GdprEncryptionConverterFactory) optionsCopy.Converters.RemoveAt(i);

            var jsonDoc = JsonSerializer.SerializeToDocument(value, optionsCopy);
            using var stream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(stream);

            jsonWriter.WriteStartObject();
            foreach (var element in jsonDoc.RootElement.EnumerateObject())
            {
                var prop = _eventType.GetProperty(element.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.PropertyType == typeof(string) && prop.IsDefined(typeof(GdprEncryptedAttribute), true))
                {
                    string rawValue = element.Value.GetString() ?? "";
                    string key = _keyVault.GetOrCreateKeyAsync(aggregateId).GetAwaiter().GetResult();
                    jsonWriter.WriteString(element.Name, CryptoEngine.Encrypt(rawValue, key));
                }
                else element.WriteTo(jsonWriter);
            }
            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            using var finalDoc = JsonDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
            finalDoc.WriteTo(writer);
        }
    }
}
