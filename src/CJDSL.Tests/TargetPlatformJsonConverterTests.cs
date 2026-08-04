using System.Text.Json;
using CJDSL.Domain;
using FluentAssertions;
using Xunit;

namespace CJDSL.Tests;

/// <summary>
/// 验证 TargetPlatform 的 JSON 转换器：字符串（大小写不敏感）、数字、未知字符串
/// 三种形态均可正确反序列化，避免 LLM 输出格式差异导致整段 DSL 反序列化失败。
/// </summary>
public class TargetPlatformJsonConverterTests
{
    private sealed class Holder
    {
        public TargetPlatform TargetPlatform { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    [Theory]
    [InlineData("\"Web\"", TargetPlatform.Web)]
    [InlineData("\"web\"", TargetPlatform.Web)]
    [InlineData("\"WEB\"", TargetPlatform.Web)]
    [InlineData("\"Wpf\"", TargetPlatform.Wpf)]
    [InlineData("\"Maui\"", TargetPlatform.Maui)]
    [InlineData("\"React\"", TargetPlatform.React)]
    [InlineData("\"Vue\"", TargetPlatform.Vue)]
    public void 字符串形态_大小写不敏感_可反序列化(string jsonValue, TargetPlatform expected)
    {
        var json = $"{{ \"targetPlatform\": {jsonValue} }}";
        var holder = JsonSerializer.Deserialize<Holder>(json, Options);
        holder!.TargetPlatform.Should().Be(expected);
    }

    [Fact]
    public void 数字形态_可反序列化()
    {
        var json = $"{{ \"targetPlatform\": {((int)TargetPlatform.Web)} }}";
        var holder = JsonSerializer.Deserialize<Holder>(json, Options);
        holder!.TargetPlatform.Should().Be(TargetPlatform.Web);
    }

    [Fact]
    public void 未知字符串_回退默认成员_不抛异常()
    {
        var json = "{ \"targetPlatform\": \"NotARealPlatform\" }";
        var act = () => JsonSerializer.Deserialize<Holder>(json, Options);
        act.Should().NotThrow();
        var holder = JsonSerializer.Deserialize<Holder>(json, Options);
        holder!.TargetPlatform.Should().Be(TargetPlatform.Web);
    }

    [Fact]
    public void 序列化_输出枚举名字符串_且可回环()
    {
        var holder = new Holder { TargetPlatform = TargetPlatform.Wpf };
        var json = JsonSerializer.Serialize(holder, Options);
        // 枚举应序列化为名称字符串（"Wpf"），而非数字，便于阅读/编辑
        json.Should().Contain("\"Wpf\"");
        var back = JsonSerializer.Deserialize<Holder>(json, Options);
        back!.TargetPlatform.Should().Be(TargetPlatform.Wpf);
    }
}
