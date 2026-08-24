using System.Collections.Generic;
using System.Threading.Tasks;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain;
using CJDSL.Generation.Services;
using FluentAssertions;
using Xunit;

namespace CJDSL.Tests;

/// <summary>
/// 模板生成器单测：覆盖 form / list / detail 生成，并校验关键 DSL 结构。
/// </summary>
public class TemplateDslGeneratorTests
{
    private static M1_Object CreateMetaObject() => new()
    {
        Code = "repair",
        Name = "设备报修单",
        Properties = new List<M1_Property>
        {
            new() { Code = "deviceName", Name = "设备名称", Type = "string", Required = true, Enabled = true },
            new() { Code = "faultDesc", Name = "故障描述", Type = "textarea", Required = true, Enabled = true },
            new() { Code = "reportDate", Name = "报修日期", Type = "date", Enabled = true },
            new() { Code = "urgent", Name = "是否紧急", Type = "boolean", Enabled = true }
        }
    };

    [Fact]
    public async Task GenerateFormAsync_生成包含表单与全部字段()
    {
        var gen = new TemplateDslGenerator();
        var dsl = await gen.GenerateFormAsync(CreateMetaObject(), new GenerateOptions());

        dsl.Layout.Should().Be("form");
        dsl.Components.Should().NotBeEmpty();
        dsl.GetAllComponents().Should().Contain(c => c.Type == "form");
        dsl.GetAllComponents().Count(c => !string.IsNullOrEmpty(c.FieldName)).Should().Be(4);
    }

    [Fact]
    public async Task GenerateListAsync_生成包含表格组件()
    {
        var gen = new TemplateDslGenerator();
        var dsl = await gen.GenerateListAsync(CreateMetaObject(), new GenerateOptions());

        dsl.Layout.Should().Be("list");
        dsl.GetAllComponents().Should().Contain(c => c.Type == "table");
    }

    [Fact]
    public async Task GenerateDetailAsync_返回非空组件树()
    {
        var gen = new TemplateDslGenerator();
        var dsl = await gen.GenerateDetailAsync(CreateMetaObject(), new GenerateOptions());

        dsl.Layout.Should().Be("detail");
        dsl.Components.Should().NotBeEmpty();
    }
}
