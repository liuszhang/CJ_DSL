using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain.Interfaces;
using CJDSL.Infrastructure.LLM;
using Microsoft.Extensions.Logging;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// 基于 LLM 的 DSL 生成器 - 通过大模型生成 DSL 页面
/// </summary>
public class LlmDslGenerator : IDslGenerator
{
    private readonly ILLMClientProvider _clientProvider;
    private readonly IDslPromptBuilder _promptBuilder;
    private readonly IDslResponseParser _responseParser;
    private readonly ILogger<LlmDslGenerator> _logger;

    public LlmDslGenerator(
        ILLMClientProvider clientProvider,
        IDslPromptBuilder promptBuilder,
        IDslResponseParser responseParser,
        ILogger<LlmDslGenerator> logger)
    {
        _clientProvider = clientProvider;
        _promptBuilder = promptBuilder;
        _responseParser = responseParser;
        _logger = logger;
    }

    public async Task<DslPage> GenerateFormAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildFormPrompt(metaObject, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, ct);
    }

    public async Task<DslPage> GenerateListAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildListPrompt(metaObject, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, ct);
    }

    public async Task<DslPage> GenerateDetailAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildFormPrompt(metaObject, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, ct);
    }

    public async Task<DslPage> GenerateFromNlpAsync(string description, UserContext user, GenerateOptions options, CancellationToken ct = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildNlpPrompt(description, user, options);

        return await GenerateFromLlmAsync(systemPrompt, userPrompt, ct);
    }

    public Task<DslPage> GenerateDashboardAsync(M4_Scene scene, GenerateOptions options, CancellationToken ct = default)
    {
        // Dashboard generation - simplified
        var dsl = new DslPage
        {
            Id = $"dashboard_{scene?.Code ?? "default"}",
            Title = scene?.Name ?? "仪表盘",
            Description = scene?.Description ?? "",
            Layout = "dashboard",
            Components = new List<DslComponent>
            {
                new()
                {
                    Type = "grid",
                    Children = new List<DslComponent>
                    {
                        new()
                        {
                            Type = "card",
                            Props = new Dictionary<string, object> { { "Elevation", 2 } },
                            Children = new List<DslComponent>
                            {
                                new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h5" } }, Label = "仪表盘" }
                            }
                        }
                    }
                }
            }
        };
        return Task.FromResult(dsl);
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

    private async Task<DslPage> GenerateFromLlmAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        try
        {
            var client = _clientProvider.GetClient();
            _logger.LogInformation("Using LLM provider: {Provider}", client.Provider);

            var response = await client.GenerateAsync(new LLMRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.3f,
                MaxTokens = 4096,
                JsonMode = true
            }, ct);

            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.RawText))
            {
                _logger.LogWarning("LLM generation failed: {Error}", response.ErrorMessage);
                return CreateFallbackPage();
            }

            var dslPage = _responseParser.Parse(response.RawText);
            if (dslPage == null)
            {
                _logger.LogWarning("Failed to parse LLM response as DSL");
                return CreateFallbackPage();
            }

            return dslPage;
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
