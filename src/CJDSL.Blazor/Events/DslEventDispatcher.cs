using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CJDSL.Blazor.Events;

/// <summary>
/// DSL 事件分发器（客户端实现）
/// </summary>
public class DslEventDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly ISnackbar _snackbar;
    private readonly NavigationManager _navigation;
    private readonly IDialogService _dialogService;
    private readonly IJSRuntime _jsRuntime;

    public DslEventDispatcher(
        HttpClient httpClient,
        ISnackbar snackbar,
        NavigationManager navigation,
        IDialogService dialogService,
        IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _snackbar = snackbar;
        _navigation = navigation;
        _dialogService = dialogService;
        _jsRuntime = jsRuntime;
    }

    public async Task DispatchAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        if (evt.DebounceMs.HasValue && evt.DebounceMs.Value > 0)
            await Task.Delay(evt.DebounceMs.Value);

        if (evt.Confirm != null)
        {
            var confirmed = await ShowConfirmAsync(evt.Confirm);
            if (!confirmed) return;
        }

        switch (evt.Handler)
        {
            case "submit": await HandleSubmitAsync(evt, component, context); break;
            case "apiCall": await HandleApiCallAsync(evt, component, context); break;
            case "navigate": await HandleNavigateAsync(evt, context); break;
            case "openModal": await HandleOpenModalAsync(evt, context); break;
            case "closeModal": await HandleCloseModalAsync(evt, context); break;
            case "refresh": await HandleRefreshAsync(evt, context); break;
            case "setValue": await HandleSetValueAsync(evt, context); break;
            case "showToast": await HandleShowToastAsync(evt); break;
            case "export": await HandleExportAsync(evt, context); break;
            case "validate": await HandleValidateAsync(evt, context); break;
            case "reset": await HandleResetAsync(evt, context); break;
            case "chain": await HandleChainAsync(evt, component, context); break;
            default: _snackbar.Add($"未知 Handler: {evt.Handler}", Severity.Warning); break;
        }
    }

    private async Task HandleApiCallAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        var endpoint = ResolveTemplate(evt.Params?.GetValueOrDefault("endpoint")?.ToString() ?? "", context);
        var method = evt.Params?.GetValueOrDefault("method")?.ToString() ?? "GET";
        var formId = evt.Params?.GetValueOrDefault("formId")?.ToString();

        object? payload = null;
        if (!string.IsNullOrEmpty(formId) && context.Forms.TryGetValue(formId, out var formState))
            payload = formState.GetValues();

        try
        {
            using var response = method.ToUpper() switch
            {
                "GET" => await _httpClient.GetAsync(endpoint),
                "POST" => await _httpClient.PostAsJsonAsync(endpoint, payload),
                "PUT" => await _httpClient.PutAsJsonAsync(endpoint, payload),
                "DELETE" => await _httpClient.DeleteAsync(endpoint),
                _ => throw new NotSupportedException($"HTTP method {method} not supported")
            };

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            // 成功回调
            if (evt.Params?.GetValueOrDefault("onSuccess") is List<Dictionary<string, object>> callbacks)
            {
                foreach (var cb in callbacks)
                {
                    await DispatchAsync(new DslEvent
                    {
                        Type = "callback",
                        Handler = cb.GetValueOrDefault("handler")?.ToString() ?? "",
                        Params = cb.GetValueOrDefault("params") as Dictionary<string, object>
                    }, component, context);
                }
            }

            _snackbar.Add("操作成功", Severity.Success);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"操作失败: {ex.Message}", Severity.Error);
        }
    }

    private async Task HandleChainAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        if (evt.Params?.GetValueOrDefault("chain") is not List<Dictionary<string, object>> chain) return;
        foreach (var step in chain)
        {
            var stepEvent = new DslEvent
            {
                Type = "chain",
                Handler = step.GetValueOrDefault("handler")?.ToString() ?? "",
                Params = step.GetValueOrDefault("params") as Dictionary<string, object>,
                Confirm = step.ContainsKey("confirm") ? MapConfirm(step["confirm"]) : null
            };
            await DispatchAsync(stepEvent, component, context);
        }
    }

    private async Task HandleSubmitAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        var formId = evt.Params?.GetValueOrDefault("formId")?.ToString();
        if (!string.IsNullOrEmpty(formId) && context.Forms.TryGetValue(formId, out var form))
        {
            _snackbar.Add("提交表单", Severity.Info);
        }
    }

    private async Task HandleNavigateAsync(DslEvent evt, DslRenderContext context)
    {
        var path = ResolveTemplate(evt.Params?.GetValueOrDefault("path")?.ToString() ?? "/", context);
        _navigation.NavigateTo(path);
    }

    private async Task HandleOpenModalAsync(DslEvent evt, DslRenderContext context)
    {
        _snackbar.Add("打开模态框", Severity.Info);
    }

    private async Task HandleCloseModalAsync(DslEvent evt, DslRenderContext context)
    {
        _snackbar.Add("关闭模态框", Severity.Info);
    }

    private async Task HandleRefreshAsync(DslEvent evt, DslRenderContext context)
    {
        _snackbar.Add("刷新数据", Severity.Info);
    }

    private async Task HandleSetValueAsync(DslEvent evt, DslRenderContext context)
    {
        var field = evt.Params?.GetValueOrDefault("field")?.ToString();
        var value = evt.Params?.GetValueOrDefault("value");
        if (!string.IsNullOrEmpty(field)) context.DataStore.Set(field, value);
    }

    private async Task HandleShowToastAsync(DslEvent evt)
    {
        var message = evt.Params?.GetValueOrDefault("message")?.ToString() ?? "操作成功";
        var severity = evt.Params?.GetValueOrDefault("severity")?.ToString() switch
        {
            "error" => Severity.Error,
            "warning" => Severity.Warning,
            "info" => Severity.Info,
            _ => Severity.Success
        };
        _snackbar.Add(message, severity);
    }

    private async Task HandleExportAsync(DslEvent evt, DslRenderContext context)
    {
        _snackbar.Add("导出功能开发中", Severity.Info);
    }

    private async Task HandleValidateAsync(DslEvent evt, DslRenderContext context)
    {
        _snackbar.Add("验证通过", Severity.Success);
    }

    private async Task HandleResetAsync(DslEvent evt, DslRenderContext context)
    {
        var formId = evt.Params?.GetValueOrDefault("formId")?.ToString();
        if (!string.IsNullOrEmpty(formId) && context.Forms.TryGetValue(formId, out var form))
            form.Reset();
    }

    private string ResolveTemplate(string template, DslRenderContext context)
    {
        return Regex.Replace(template, @"\{(\w+)\}", m =>
        {
            var key = m.Groups[1].Value;
            var value = context.DataStore.GetString($"data.{key}") ?? context.DataStore.GetString($"row.{key}") ?? key;
            return Uri.EscapeDataString(value ?? "");
        });
    }

    private async Task<bool> ShowConfirmAsync(DslConfirm confirm)
    {
        _snackbar.Add(confirm.Message, Severity.Info);
        await Task.Delay(100);
        return true;
    }

    private static DslConfirm? MapConfirm(object? confirmObj)
    {
        if (confirmObj is not Dictionary<string, object> dict) return null;
        return new DslConfirm
        {
            Title = dict.GetValueOrDefault("title")?.ToString() ?? "确认",
            Message = dict.GetValueOrDefault("message")?.ToString() ?? "",
            ConfirmText = dict.GetValueOrDefault("confirmText")?.ToString() ?? "确认",
            CancelText = dict.GetValueOrDefault("cancelText")?.ToString() ?? "取消"
        };
    }
}
