using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSSharp.ModuleFactory
{
    internal sealed class JsonStringVersionConverter : JsonConverter<Version>
    {
        public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var versionString = reader.GetString();
            if (versionString is null) return null;
            return Version.Parse(versionString);
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
