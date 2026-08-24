using CJCore.LLM.Abstractions;
using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain.Interfaces;
using CJDSL.Generation.LLM;
using Microsoft.Extensions.Logging;

namespace CJDSL.Generation.Services;

/// <summary>
/// 基于 LLM 的 DSL 生成器 - 通过大模型生成 DSL 页面。
/// 模块 J：LLM 调用收敛到 CJCore —— 使用 IStructuredLLMClient 强类型结构化输出
/// （内置 markdown 围栏剥离 / JSON 注释清洗 / 反序列化），取代自建裸文本解析。
/// </summary>
public class LlmDslGenerator : IDslGenerator
{
    private readonly IStructuredLLMClient _structuredClient;
    private readonly IDslPromptBuilder _promptBuilder;
    private readonly ILogger<LlmDslGenerator> _logger;

    public LlmDslGenerator(
        IStructuredLLMClient structuredClient,
        IDslPromptBuilder promptBuilder,
        ILogger<LlmDslGenerator> logger)
    {
        _structuredClient = structuredClient;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<DslPage> GenerateFormAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildFormPrompt(metaObject, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, options.TargetPlatform, ct);
    }

    public async Task<DslPage> GenerateListAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildListPrompt(metaObject, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, options.TargetPlatform, ct);
    }

    public async Task<DslPage> GenerateDetailAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildFormPrompt(metaObject, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, options.TargetPlatform, ct);
    }

    public async Task<DslPage> GenerateFromNlpAsync(string description, UserContext user, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildNlpPrompt(description, user, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, options.TargetPlatform, ct);
    }

    public async Task<DslPage> GenerateDashboardAsync(M4_Scene scene, GenerateOptions options, CancellationToken ct = default)
    {
        try
        {
            var systemPrompt = _promptBuilder.BuildSystemPrompt();
            var userPrompt = _promptBuilder.BuildDashboardPrompt(scene, options);

            var result = await _structuredClient.SendStructuredAsync<DslPage>(
                systemPrompt, userPrompt, temperature: 0.3, maxTokens: 4096, ct: ct);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("LLM 仪表盘生成失败: {Error}", result.Error);
                return CreateDashboardFallback(scene);
            }

            _logger.LogInformation(
                "LLM 仪表盘生成成功（PromptTokens={Prompt}, CompletionTokens={Completion}）",
                result.PromptTokens, result.CompletionTokens);
            if (result.Data != null && string.IsNullOrEmpty(result.Data.RendererHint))
                result.Data.RendererHint = GenerateOptions.ResolveRendererHint(options.TargetPlatform);
            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM dashboard generation");
            return CreateDashboardFallback(scene);
        }
    }

    public async Task<DslPage> AdaptAsync(DslPage baseDsl, UserContext user, DataContext data, CancellationToken ct = default)
    {
        // Adapt DSL based on user context
        var adapted = CloneDslPage(baseDsl);

        foreach (var component in adapted.GetAllComponents())
        {
            // Apply visibility based on user roles
            if (component.VisibleIf != null && component.VisibleIf.Contains("user"))
            {
                if (!user.Roles.Contains("admin") && component.VisibleIf.Contains("admin"))
                {
                    component.VisibleIf = "false";
                }
            }
        }

        return await Task.FromResult(adapted);
    }

    private async Task<DslPage> GenerateFromLlmAsync(string systemPrompt, string userPrompt, TargetPlatform platform, CancellationToken ct)
    {
        try
        {
            var result = await _structuredClient.SendStructuredAsync<DslPage>(
                systemPrompt, userPrompt, temperature: 0.3, maxTokens: 4096, ct: ct);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("LLM 结构化生成失败: {Error}", result.Error);
                return CreateFallbackPage();
            }

            _logger.LogInformation(
                "LLM DSL 生成成功（PromptTokens={Prompt}, CompletionTokens={Completion}）",
                result.PromptTokens, result.CompletionTokens);
            if (result.Data != null && string.IsNullOrEmpty(result.Data.RendererHint))
                result.Data.RendererHint = GenerateOptions.ResolveRendererHint(platform);
            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM DSL generation");
            return CreateFallbackPage();
        }
    }

    private static DslPage CreateFallbackPage()
    {
        return new DslPage
        {
            Id = $"fallback_{Guid.NewGuid():N}",
            Title = "生成失败",
            Description = "LLM 生成 DSL 失败，请重试",
            Layout = "form",
            Components = new List<DslComponent>
            {
                new()
                {
                    Type = "card",
                    Children = new List<DslComponent>
                    {
                        new()
                        {
                            Type = "textDisplay",
                            Props = new Dictionary<string, object> { { "Typo", "h5" } },
                            Label = "DSL 生成失败"
                        },
                        new()
                        {
                            Type = "textDisplay",
                            Props = new Dictionary<string, object> { { "Typo", "body1" } },
                            Label = "请检查 LLM 配置或重试"
                        }
                    }
                }
            }
        };
    }

    private static DslPage CreateDashboardFallback(M4_Scene? scene)
    {
        var title = scene?.Name ?? "数据仪表盘";
        var statCards = new List<DslComponent>
        {
            BuildStatCard("业务对象", "—", "Primary"),
            BuildStatCard("枚举项", "—", "Secondary"),
            BuildStatCard("字典项", "—", "Tertiary"),
            BuildStatCard("今日待办", "—", "Info")
        };

        return new DslPage
        {
            Id = $"dashboard_{scene?.Code ?? "default"}",
            Title = title,
            Description = scene?.Description ?? "基于元模型统计的仪表盘（LLM 生成失败，使用模板回退）",
            Layout = "dashboard",
            Components = new List<DslComponent>
            {
                new()
                {
                    Type = "grid",
                    Props = new Dictionary<string, object> { { "Spacing", 3 } },
                    Children = statCards
                },
                new()
                {
                    Type = "card",
                    Props = new Dictionary<string, object> { { "Elevation", 2 }, { "Class", "mt-4" } },
                    Children = new List<DslComponent>
                    {
                        new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h6" } }, Label = "趋势图" },
                        new()
                        {
                            Type = "chart",
                            Props = new Dictionary<string, object> { { "ChartType", "line" }, { "Title", "近 7 日趋势" }, { "Height", "280" } }
                        }
                    }
                },
                new()
                {
                    Type = "card",
                    Props = new Dictionary<string, object> { { "Elevation", 2 }, { "Class", "mt-4" } },
                    Children = new List<DslComponent>
                    {
                        new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h6" } }, Label = "最近记录" },
                        new()
                        {
                            Type = "list",
                            Children = new List<DslComponent>
                            {
                                new() { Type = "listItem", Label = "暂无数据" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static DslComponent BuildStatCard(string label, string value, string color) => new()
    {
        Type = "card",
        Props = new Dictionary<string, object> { { "Elevation", 2 }, { "Class", "pa-3" } },
        Children = new List<DslComponent>
        {
            new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "body2" }, { "Color", color } }, Label = label },
            new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h3" } }, Label = value }
        }
    };

    private static DslPage CloneDslPage(DslPage source)
    {
        return new DslPage
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            Layout = source.Layout,
            TargetPlatform = source.TargetPlatform,
            Components = source.Components,
            DataSource = source.DataSource,
            Permission = source.Permission,
            Responsive = source.Responsive,
            Style = source.Style,
            PageEvents = source.PageEvents
        };
    }
}
