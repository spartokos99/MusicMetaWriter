using System.Text.Json;
using System.Text.Json.Serialization;
using MusicMetaWriter.Models;

namespace MusicMetaWriter_CP.Models;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettingsModel))]
partial class AppSettingsModelJsonContext : JsonSerializerContext
{
}