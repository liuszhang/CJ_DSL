using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CJCore.LLM.Abstractions;
using CJCore.LLM.LLMClient;
using CJCore.LLM.Structured;
using CJDSL.Domain.Interfaces;
using CJDSL.Generation.LLM;
using CJDSL.Generation.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CJDSL.Tests;

/// <summary>
/// 模块 J（LLM 收敛到 CJCore）核心逻辑单测：
/// - DbConfiguredLLMClient 从 DB 默认模型配置填充请求 / 无配置时返回失败且不发起网络请求
/// - DslGeneratorResolver 在 DB 未配置默认模型时降级为模板生成器
/// 不依赖真实 LLM 网络调用。
/// </summary>
public class LlmConvergenceTests
{
    private static Mock<ILLMConfigReader> ReaderReturning(string? endpoint, string? apiKey, string? model)
    {
        var mock = new Mock<ILLMConfigReader>();
        mock.Setup(r => r.GetDefaultModelConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((endpoint, apiKey, model));
        return mock;
    }

    /// <summary>记录内部客户端是否真正发起了 HTTP 请求，用于验证 DB 配置已被填充。</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            var json = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"ok\"}}],\"usage\":{\"prompt_tokens\":1,\"completion_tokens\":1}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }

    [Fact]
    public async Task DbConfiguredLLMClient_有默认配置_填充请求并调用内部客户端()
    {
        var reader = ReaderReturning("http://llm.local/v1", "secret", "gpt-4o");
        var handler = new RecordingHandler();
        var client = new DbConfiguredLLMClient(
            reader.Object,
            new HttpClient(handler),
            Mock.Of<ILogger<DbConfiguredLLMClient>>());

        var request = new ChatRequest
        {
            Messages = new List<ChatMessage> { new() { Role = "user", Content = "hi" } }
        };
        await client.CompleteAsync(request);

        handler.WasCalled.Should().BeTrue("应已用 DB 默认模型配置填充 Endpoint 并调用内部客户端");
        request.Endpoint.Should().Be("http://llm.local/v1");
        request.Model.Should().Be("gpt-4o");
        request.ApiKey.Should().Be("secret");
    }

    [Fact]
    public async Task DbConfiguredLLMClient_无默认配置_返回失败且不发起网络请求()
    {
        var reader = ReaderReturning(null, null, null);
        var handler = new RecordingHandler();
        var client = new DbConfiguredLLMClient(
            reader.Object,
            new HttpClient(handler),
            Mock.Of<ILogger<DbConfiguredLLMClient>>());

        var resp = await client.CompleteAsync(new ChatRequest());

        handler.WasCalled.Should().BeFalse("无默认模型时不应调用内部客户端");
        resp.IsSuccess.Should().BeFalse();
        resp.Error.Should().Contain("未配置默认 LLM 模型");
    }

    [Fact]
    public async Task DbConfiguredLLMClient_IsAvailableAsync_随DB配置变化()
    {
        var reader = ReaderReturning("http://llm.local/v1", null, "gpt-4o");
        var client = new DbConfiguredLLMClient(
            reader.Object,
            new HttpClient(new RecordingHandler()),
            Mock.Of<ILogger<DbConfiguredLLMClient>>());

        (await client.IsAvailableAsync()).Should().BeTrue();

        reader.Setup(r => r.GetDefaultModelConfigAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync((null, null, null));
        (await client.IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public void DslGeneratorResolver_LLM可用_返回LlmDslGenerator()
    {
        var llmClient = new Mock<ILLMClient>();
        llmClient.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resolver = new DslGeneratorResolver(
            new TemplateDslGenerator(),
            new LlmDslGenerator(Mock.Of<IStructuredLLMClient>(), Mock.Of<IDslPromptBuilder>(), Mock.Of<ILogger<LlmDslGenerator>>()),
            llmClient.Object,
            Mock.Of<ILogger<DslGeneratorResolver>>());

        resolver.Resolve("llm").Should().BeOfType<LlmDslGenerator>();
    }

    [Fact]
    public void DslGeneratorResolver_LLM不可用_降级为TemplateDslGenerator()
    {
        var llmClient = new Mock<ILLMClient>();
        llmClient.Setup(c => c.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var resolver = new DslGeneratorResolver(
            new TemplateDslGenerator(),
            new LlmDslGenerator(Mock.Of<IStructuredLLMClient>(), Mock.Of<IDslPromptBuilder>(), Mock.Of<ILogger<LlmDslGenerator>>()),
            llmClient.Object,
            Mock.Of<ILogger<DslGeneratorResolver>>());

        resolver.Resolve("llm").Should().BeOfType<TemplateDslGenerator>();
    }
}
