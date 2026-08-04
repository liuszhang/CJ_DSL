using System.Collections.Generic;
using System.Threading.Tasks;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CJDSL.Tests;

/// <summary>
/// 安全校验器单测：覆盖沙箱超时、endpoint 白名单、富文本 XSS 清洗三项验收标准。
/// </summary>
public class SecurityValidatorTests
{
    private readonly DslSecurityValidator _validator = new();

    [Fact]
    public async Task 恶意死循环表达式_被沙箱超时拒绝()
    {
        var dsl = new DslPage
        {
            Components = new List<DslComponent>
            {
                new() { Id = "c1", Type = "text", VisibleIf = "while(true){}" }
            }
        };

        var result = await _validator.ValidateAsync(dsl);

        result.IsSafe.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("VisibleIf"));
    }

    [Fact]
    public async Task 外部绝对endpoint_被白名单拦截()
    {
        var dsl = new DslPage
        {
            Components = new List<DslComponent>
            {
                new()
                {
                    Id = "c1", Type = "button",
                    Events = new List<DslEvent>
                    {
                        new() { Type = "onClick", Handler = "apiCall", Params = new Dictionary<string, object> { { "endpoint", "https://evil.com/steal" } } }
                    }
                }
            }
        };

        var result = await _validator.ValidateAsync(dsl);

        result.IsSafe.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("未授权"));
    }

    [Fact]
    public async Task 富文本中的script_被清洗()
    {
        var dsl = new DslPage
        {
            Components = new List<DslComponent>
            {
                new()
                {
                    Id = "c1", Type = "richText",
                    Props = new Dictionary<string, object> { { "Content", "<div onclick=\"x()\">hi</div><script>alert(1)</script>" } }
                }
            }
        };

        var sanitized = await _validator.SanitizeAsync(dsl);
        var content = sanitized.Components[0].Props!["Content"].ToString();

        content.Should().NotContain("<script>");
    }

    [Fact]
    public async Task 同源相对endpoint_允许通过()
    {
        var dsl = new DslPage
        {
            Components = new List<DslComponent>
            {
                new()
                {
                    Id = "c1", Type = "button",
                    Events = new List<DslEvent>
                    {
                        new() { Type = "onClick", Handler = "apiCall", Params = new Dictionary<string, object> { { "endpoint", "/api/repair/save" } } }
                    }
                }
            }
        };

        var result = await _validator.ValidateAsync(dsl);
        result.IsSafe.Should().BeTrue();
    }
}
