using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuliERP.Api.Kernel;

/// <summary>
/// Snowflake / HiLo ID wire-format contract (API-CONTRACT-ID-001).
///
/// <para>
/// All <c>long</c> properties on the public wire (DTOs, request
/// bodies, Auth <c>/me</c>, etc.) are serialized as <b>JSON strings</b>
/// — NEVER as JSON numbers — to prevent JavaScript precision loss.
/// JavaScript's <c>Number.MAX_SAFE_INTEGER</c> is 2^53 - 1
/// (= 9_007_199_254_740_991), and the GuliERP HiLo sequence
/// (<c>identity.gulierp_hilo_sequence</c>) routinely produces
/// values above that (the user reported a real UOM id
/// <c>83727350616817740</c>). When a JS client parses the JSON,
/// the long is silently rounded, the round-trip id no longer
/// matches the database row, and the next <c>GET /.../{id}</c>
/// returns 404.
/// </para>
///
/// <para>
/// <b>Wire contract</b>:
/// <list type="bullet">
///   <item>OUTPUT: a 64-bit signed decimal integer rendered as a
///         JSON <c>STRING</c> token (e.g. <c>"83727350616817740"</c>).
///         Use <see cref="CultureInfo.InvariantCulture"/> so
///         the wire is locale-independent (no thousand separators,
///         no narrow no-break space).</item>
///   <item>INPUT: a JSON string with the same shape. For backward
///         compatibility with already-shipped clients we ALSO
///         accept a JSON number; if the value exceeds 2^53 it
///         would round-trip as a number but the explicit string
///         path is the recommended one.</item>
///   <item>NULLABLE: <see cref="NullableSnowflakeLongJsonConverter"/>
///         handles <c>long?</c> — null / missing → null; string
///         → <c>long</c>; number → <c>long</c>.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Scope</b>: applied to ALL <c>long</c> / <c>long?</c> properties
/// in the API project (controller-bound and minimal-API-bound
/// response and request bodies). The single
/// <c>long</c> field that is NOT an id and therefore MUST stay
/// a number on the wire — <c>PagedResult&lt;T&gt;.TotalCount</c> —
/// is declared as <c>int</c> so the converter does not match it.
/// </para>
///
/// <para>
/// <b>What this converter does NOT touch</b>:
/// <list type="bullet">
///   <item><c>int</c> / <c>int?</c> — unchanged. <c>ConcurrencyVersion</c>
///         (V1 mdm contracts), <c>Page</c>, <c>PageSize</c>,
///         <c>Status</c>, the role / permission enums, etc. all
///         remain JSON numbers.</item>
///   <item><c>decimal</c> / <c>double</c> / <c>float</c> — unchanged.
///         Business amounts, quantities, exchange rates are
///         not IDs.</item>
///   <item><c>DateTimeOffset</c> / <c>DateTime</c> / <c>Guid</c> —
///         unchanged.</item>
///   <item>Database / EF / Domain / Service signatures — unchanged.
///         The Domain ID remains <c>long</c>; the converter only
///         acts on the wire boundary.</item>
///   <item>Route template parameters — the ASP.NET Core route
///         binder parses a URL segment as a <c>long</c> via
///         <c>TypeConverter</c>; this is independent of JSON
///         serialization. The string segment
///         <c>/api/v1/mdm/uoms/83727350616817740</c> is bound
///         to <c>long id</c> without loss.</item>
/// </list>
/// </para>
///
/// <para>
/// See <c>docs/verification/API_CONTRACT_ID_001_REPORT.md</c>
/// for the test matrix and the audit trail.
/// </para>
/// </summary>
public sealed class SnowflakeLongJsonConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
            {
                var raw = reader.GetString();
                if (string.IsNullOrEmpty(raw))
                {
                    throw new JsonException(
                        "Snowflake long cannot be deserialized from an empty string.");
                }
                if (long.TryParse(
                        raw,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var v))
                {
                    return v;
                }
                throw new JsonException(
                    $"Snowflake long '{raw}' is not a valid 64-bit signed integer.");
            }
            case JsonTokenType.Number:
                // Backward-compat: accept numbers (with the known
                // 2^53 ceiling for JS clients; long values above
                // 2^53 will already be lost on the JS side, but
                // we still accept them here for tests + non-JS
                // clients like curl / Postman).
                return reader.GetInt64();
            default:
                throw new JsonException(
                    $"Snowflake long cannot be deserialized from {reader.TokenType}; " +
                    "expected a JSON string (preferred) or number.");
        }
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        // CRITICAL: must be a string, not a number, to avoid JS
        // precision loss. InvariantCulture so wire is
        // locale-independent (no ',' decimal separator, no
        // narrow no-break space thousands separator).
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Nullable counterpart of <see cref="SnowflakeLongJsonConverter"/>.
/// Mirrors the same wire rules; additionally honors
/// <see cref="JsonTokenType.Null"/> and the
/// <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/> /
/// <c>[JsonIgnore]</c> semantics.
/// </summary>
public sealed class NullableSnowflakeLongJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }
            if (long.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var v))
            {
                return v;
            }
            throw new JsonException(
                $"Snowflake nullable long '{raw}' is not a valid 64-bit signed integer.");
        }
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }
        throw new JsonException(
            $"Snowflake nullable long cannot be deserialized from {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
