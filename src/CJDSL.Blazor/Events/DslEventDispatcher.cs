using CJDSL.Blazor.Components;
using CJDSL.Blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CJDSL.Blazor.Events;

/// <summary>
/// DSL 事件分发器（客户端实现）。
/// DispatchAsync 返回 false 表示事件被取消或执行失败，chain 链路会据此中断后续步骤。
/// </summary>
public class DslEventDispatcher : IDslEventDispatcher
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

    public async Task<bool> DispatchAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        if (evt.DebounceMs.HasValue && evt.DebounceMs.Value > 0)
            await Task.Delay(evt.DebounceMs.Value);

        if (evt.Confirm != null)
        {
            var confirmed = await ShowConfirmAsync(evt.Confirm);
            if (!confirmed) return false;
        }

        switch (evt.Handler)
        {
            case "submit": return await HandleSubmitAsync(evt, component, context);
            case "apiCall": return await HandleApiCallAsync(evt, component, context);
            case "navigate": return await HandleNavigateAsync(evt, context);
            case "openModal": return await HandleOpenModalAsync(evt, context);
            case "closeModal": return await HandleCloseModalAsync(evt, context);
            case "refresh": return await HandleRefreshAsync(evt, context);
            case "setValue": return await HandleSetValueAsync(evt, context);
            case "showToast": return await HandleShowToastAsync(evt);
            case "export": return await HandleExportAsync(evt, context);
            case "validate": return await HandleValidateAsync(evt, context);
            case "reset": return await HandleResetAsync(evt, context);
            case "chain": return await HandleChainAsync(evt, component, context);
            default:
                _snackbar.Add($"未知 Handler: {evt.Handler}", Severity.Warning);
                return false;
        }
    }

    private async Task<bool> HandleApiCallAsync(DslEvent evt, DslComponent component, DslRenderContext context)
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
            return true;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"操作失败: {ex.Message}", Severity.Error);
            return false;
        }
    }

    private async Task<bool> HandleChainAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        if (evt.Params?.GetValueOrDefault("chain") is not List<Dictionary<string, object>> chain) return true;
        foreach (var step in chain)
        {
            var stepEvent = new DslEvent
            {
                Type = "chain",
                Handler = step.GetValueOrDefault("handler")?.ToString() ?? "",
                Params = step.GetValueOrDefault("params") as Dictionary<string, object>,
                Confirm = step.ContainsKey("confirm") ? MapConfirm(step["confirm"]) : null
            };

            // 链中任一步骤失败/取消，中断后续步骤
            var ok = await DispatchAsync(stepEvent, component, context);
            if (!ok) return false;
        }
        return true;
    }

    /// <summary>
    /// 提交：校验表单 → 收集表单数据 → POST 到 endpoint（如提供）→ 触发 onSuccess 回调。
    /// </summary>
    private async Task<bool> HandleSubmitAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        var formId = evt.Params?.GetValueOrDefault("formId")?.ToString();
        if (string.IsNullOrEmpty(formId) || !context.Forms.TryGetValue(formId, out var formState))
        {
            _snackbar.Add($"未找到表单: {formId}", Severity.Warning);
            return false;
        }

        // 1. 校验（找到对应 MudForm 则执行真实校验）
        if (!await ValidateFormAsync(formId, context))
        {
            _snackbar.Add("表单校验未通过，请检查输入", Severity.Warning);
            return false;
        }

        // 2. 提交到 endpoint（未提供 endpoint 时仅完成校验和数据收集）
        var endpoint = ResolveTemplate(evt.Params?.GetValueOrDefault("endpoint")?.ToString() ?? "", context);
        if (string.IsNullOrEmpty(endpoint))
        {
            _snackbar.Add("表单校验通过", Severity.Success);
            return true;
        }

        try
        {
            var payload = formState.GetValues();
            using var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
            response.EnsureSuccessStatusCode();

            // 3. 成功回调
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

            _snackbar.Add("提交成功", Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"提交失败: {ex.Message}", Severity.Error);
            return false;
        }
    }

    private Task<bool> HandleNavigateAsync(DslEvent evt, DslRenderContext context)
    {
        var path = ResolveTemplate(evt.Params?.GetValueOrDefault("path")?.ToString() ?? "/", context);
        _navigation.NavigateTo(path);
        return Task.FromResult(true);
    }

    // openModal/closeModal/refresh/export —— Phase 2 实现
    private async Task<bool> HandleOpenModalAsync(DslEvent evt, DslRenderContext context)
    {
        DslComponent? dialogContent = null;
        var contentJson = evt.Params?.GetValueOrDefault("content")?.ToString();
        if (!string.IsNullOrEmpty(contentJson))
        {
            try { dialogContent = JsonSerializer.Deserialize<DslComponent>(contentJson); }
            catch { dialogContent = null; }
        }

        var parameters = new DialogParameters
        {
            { "Content", dialogContent },
            { "EventDispatcher", context.EventDispatcher }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        var dialog = await _dialogService.ShowAsync<DslDialog>("对话框", parameters, options);
        var result = await dialog.Result;
        return !result.Canceled;
    }

    private Task<bool> HandleCloseModalAsync(DslEvent evt, DslRenderContext context)
    {
        if (context.DialogInstance != null)
        {
            context.DialogInstance.Close();
            return Task.FromResult(true);
        }

        _snackbar.Add("当前不在对话框上下文中，无法关闭", Severity.Warning);
        return Task.FromResult(false);
    }

    private async Task<bool> HandleRefreshAsync(DslEvent evt, DslRenderContext context)
    {
        if (context.OnRefresh != null)
            await context.OnRefresh();

        // 写入刷新时间戳，触发依赖该值的表达式重新求值
        context.DataStore.Set("__refreshTick", DateTime.Now);
        _snackbar.Add("已刷新", Severity.Success);
        return true;
    }

    private Task<bool> HandleSetValueAsync(DslEvent evt, DslRenderContext context)
    {
        var field = evt.Params?.GetValueOrDefault("field")?.ToString();
        var value = evt.Params?.GetValueOrDefault("value");
        if (!string.IsNullOrEmpty(field)) context.DataStore.Set(field, value);
        return Task.FromResult(true);
    }

    private Task<bool> HandleShowToastAsync(DslEvent evt)
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
        return Task.FromResult(true);
    }

    private async Task<bool> HandleExportAsync(DslEvent evt, DslRenderContext context)
    {
        var formId = evt.Params?.GetValueOrDefault("formId")?.ToString();
        var fileName = evt.Params?.GetValueOrDefault("fileName")?.ToString() ?? "export.csv";

        List<Dictionary<string, object>> rows;
        if (!string.IsNullOrEmpty(formId) && context.Forms.TryGetValue(formId, out var form))
            rows = new List<Dictionary<string, object>> { form.GetValues() };
        else
            rows = context.DataStore.GetList<Dictionary<string, object>>("datasource.items") ?? new List<Dictionary<string, object>>();

        if (rows.Count == 0)
        {
            _snackbar.Add("没有可导出的数据", Severity.Warning);
            return false;
        }

        var csv = BuildCsv(rows);
        await _jsRuntime.InvokeVoidAsync("CJDSL.downloadFile", fileName, csv);
        _snackbar.Add($"已导出 {rows.Count} 行到 {fileName}", Severity.Success);
        return true;
    }

    private static string BuildCsv(List<Dictionary<string, object>> rows)
    {
        if (rows.Count == 0) return string.Empty;
        var headers = rows.SelectMany(r => r.Keys).Distinct().ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsv(row.TryGetValue(h, out var v) ? v?.ToString() ?? "" : ""))));
        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    /// <summary>
    /// 校验：执行 MudForm 真实校验并反馈结果。
    /// </summary>
    private async Task<bool> HandleValidateAsync(DslEvent evt, DslRenderContext context)
    {
        var formId = evt.Params?.GetValueOrDefault("formId")?.ToString();
        if (string.IsNullOrEmpty(formId))
        {
            _snackbar.Add("validate 缺少 formId 参数", Severity.Warning);
            return false;
        }

        var isValid = await ValidateFormAsync(formId, context);
        if (isValid)
            _snackbar.Add("验证通过", Severity.Success);
        else
            _snackbar.Add("表单校验未通过，请检查输入", Severity.Warning);
        return isValid;
    }

    private async Task<bool> HandleResetAsync(DslEvent evt, DslRenderContext context)
    {
        var formId = evt.Params?.GetValueOrDefault("formId")?.ToString();
        if (!string.IsNullOrEmpty(formId) && context.Forms.TryGetValue(formId, out var form))
            form.Reset();

        // 同步重置 MudForm 的 UI 校验状态
        if (!string.IsNullOrEmpty(formId)
            && context.ComponentRefs.TryGetValue(formId, out var refObj)
            && refObj is MudForm mudForm)
        {
            await mudForm.ResetValidationAsync();
        }
        return true;
    }

    /// <summary>
    /// 执行 MudForm 校验；若渲染上下文中没有对应 MudForm 引用，视为校验通过（宽松处理）。
    /// </summary>
    private static async Task<bool> ValidateFormAsync(string formId, DslRenderContext context)
    {
        if (context.ComponentRefs.TryGetValue(formId, out var refObj) && refObj is MudForm mudForm)
        {
            await mudForm.ValidateAsync();
            return mudForm.IsValid;
        }
        return true;
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

    /// <summary>
    /// 弹出 MudBlazor 原生确认对话框，返回用户真实选择（取消返回 false，中断后续事件链）。
    /// </summary>
    private async Task<bool> ShowConfirmAsync(DslConfirm confirm)
    {
        var result = await _dialogService.ShowMessageBoxAsync(
            confirm.Title,
            confirm.Message,
            yesText: confirm.ConfirmText,
            cancelText: confirm.CancelText);

        return result == true;
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
