using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimplePlayer.Entities;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Sound))]
[JsonSerializable(typeof(Sound[]))]
[JsonSerializable(typeof(List<Sound>))]
[JsonSerializable(typeof(SoundBoard))]
[JsonSerializable(typeof(SoundBoard[]))]
[JsonSerializable(typeof(List<SoundBoard>))]
public partial class BoardJsonContext : JsonSerializerContext
{
    
}