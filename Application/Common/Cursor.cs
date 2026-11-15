using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Common;

public static class Cursor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static string Encode<T>(T position) =>
        Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(position, SerializerOptions));

    public static bool TryDecode<T>(string cursor, [NotNullWhen(true)] out T? position)
        where T : class
    {
        try
        {
            position = JsonSerializer.Deserialize<T>(Base64Url.DecodeFromChars(cursor), SerializerOptions);
            return position is not null;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            position = null;
            return false;
        }
    }
}
