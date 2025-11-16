using Nethereum.Hex.HexTypes;
using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Revelium.Evm.Common;

public class HexBigIntegerConverter : JsonConverter<BigInteger>
{
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number && reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                string.Format("Found token {0} but expected token {1}",
                reader.TokenType,
                JsonTokenType.Number));

        using var document = JsonDocument.ParseValue(ref reader);

        var rawText = document.RootElement.GetRawText().Trim('\"');

        return new HexBigInteger(rawText).Value;
    }

    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options) =>
        writer.WriteRawValue(new HexBigInteger(value).HexValue, false);
}
