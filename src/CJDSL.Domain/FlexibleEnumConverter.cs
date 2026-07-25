using System.Text.Json;
using System.Text.Json.Serialization;

namespace CJDSL.Domain;

/// <summary>
/// 灵活枚举 JSON 转换器：字符串（大小写不敏感，如 "Web"/"web"）或数字均可反序列化；
/// 遇到未知字符串时回退到默认成员（数值 0，通常为首个成员如 Web），
/// 避免 LLM / 外部输入的枚举格式差异导致整段 DSL JSON 反序列化失败。
/// 序列化时统一输出枚举名（如 "Web"），便于阅读与编辑。
/// </summary>
public class FlexibleEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var num))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), num);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString()?.Trim();
            if (!string.IsNullOrEmpty(s) && Enum.TryParse<TEnum>(s, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            // 未知字符串回退到默认成员，保证后续渲染不中断
            return default;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
