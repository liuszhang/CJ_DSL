using System.Collections.Generic;
using System.Threading.Tasks;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Generation.Services;
using FluentAssertions;
using Xunit;

namespace CJDSL.Tests;

/// <summary>
/// 语义校验器单测：验证 Phase 2 的 allowlist 收敛（F1）——实验性组件降级为 Warning，未知组件报错。
/// </summary>
public class SemanticValidatorTests
{
    private readonly DslSemanticValidator _validator = new();

    [Fact]
    public async Task 实验性组件_仅警告不报错()
    {
        var dsl = new DslPage
        {
            Components = new List<DslComponent> { new() { Id = "c1", Type = "dataGrid" } }
        };

        var result = await _validator.ValidateAsync(dsl);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("实验性"));
    }

    [Fact]
    public async Task 未知组件类型_报错()
    {
        var dsl = new DslPage
        {
            Components = new List<DslComponent> { new() { Id = "c1", Type = "notARealType" } }
        };

        var result = await _validator.ValidateAsync(dsl);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("未识别"));
    }

    [Fact]
    public async Task 已渲染组件_通过校验()
    {
        var dsl = new DslPage
        {
            Components = new List<DslComponent>
            {
                new() { Id = "f1", Type = "form" },
                new() { Id = "t1", Type = "text" },
                new() { Id = "m1", Type = "markdown" }
            }
        };

        var result = await _validator.ValidateAsync(dsl);
        result.IsValid.Should().BeTrue();
    }
}
