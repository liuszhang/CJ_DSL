using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;

namespace CJDSL.Generation.Services;

/// <summary>
/// DSL 语义验证器
/// </summary>
public class DslSemanticValidator : IDslValidator
{
    /// <summary>渲染器已实现的组件类型（校验通过即渲染通过）</summary>
    private readonly HashSet<string> _renderedComponentTypes = new()
    {
        "page", "card", "form", "text", "number", "select", "autocomplete", "textarea",
        "date", "datetime", "time", "checkbox", "switch", "radio", "slider", "rating",
        "file", "button", "iconButton",
        "table", "list", "listItem", "tabs", "stepper", "expansion", "expansionPanel",
        "dialog", "snackbar", "progress", "chart", "markdown", "grid", "stack",
        "paper", "divider", "textDisplay", "avatar", "chip", "badge", "tooltip",
        "skeleton", "pagination", "alert", "richText"
    };

    /// <summary>实验性组件：校验通过但渲染器可能不支持，降级为 Warning</summary>
    private readonly HashSet<string> _experimentalComponentTypes = new()
    {
        "fab", "dataGrid", "appBar", "drawer", "breadcrumb", "tree", "timeline",
        "carousel", "colorPicker", "jsonEditor", "codeBlock", "kanban", "calendar",
        "map", "iframe", "custom"
    };

    private readonly List<string> _validHandlers = new()
    {
        "submit", "navigate", "apiCall", "openModal", "closeModal", "refresh", "setValue",
        "showToast", "export", "validate", "reset", "chain"
    };

    private readonly List<string> _validLayouts = new() { "form", "list", "detail", "dashboard", "custom" };

    public Task<DslValidationResult> ValidateAsync(DslPage dsl, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 验证基本字段
        if (string.IsNullOrWhiteSpace(dsl.Id))
            errors.Add("DslPage.Id 不能为空");
        if (string.IsNullOrWhiteSpace(dsl.Title))
            warnings.Add("DslPage.Title 为空");
        if (!_validLayouts.Contains(dsl.Layout))
            warnings.Add($"未知的布局类型: {dsl.Layout}");

        // 验证组件树
        if (dsl.Components.Count == 0)
            warnings.Add("Components 为空，页面将不显示任何内容");
        else
            ValidateComponents(dsl.Components, errors, warnings);

        // 验证表达式语法
        ValidateExpressions(dsl, errors);

        // 验证 Handler
        ValidateHandlers(dsl, errors);

        return Task.FromResult(new DslValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        });
    }

    private void ValidateComponents(List<DslComponent> components, List<string> errors, List<string> warnings)
    {
        foreach (var component in components)
        {
            if (string.IsNullOrWhiteSpace(component.Id))
                errors.Add($"组件 Id 不能为空 (Type: {component.Type})");

            if (!_renderedComponentTypes.Contains(component.Type) && !_experimentalComponentTypes.Contains(component.Type))
                errors.Add($"未识别的组件类型: {component.Type} (Id: {component.Id})");
            else if (_experimentalComponentTypes.Contains(component.Type))
                warnings.Add($"实验性组件类型（渲染器可能不支持，建议改用其他类型）: {component.Type} (Id: {component.Id})");

            if (component.Children?.Count > 0)
                ValidateComponents(component.Children, errors, warnings);
        }
    }

    private void ValidateExpressions(DslPage dsl, List<string> errors)
    {
        foreach (var component in dsl.GetAllComponents())
        {
            if (!string.IsNullOrWhiteSpace(component.VisibleIf))
            {
                try { DslSecurityValidator.CreateSandboxedEngine().Evaluate(component.VisibleIf); } catch (Exception ex)
                { errors.Add($"VisibleIf 表达式语法错误 (Id: {component.Id}): {ex.Message}"); }
            }
            if (!string.IsNullOrWhiteSpace(component.DisabledIf))
            {
                try { DslSecurityValidator.CreateSandboxedEngine().Evaluate(component.DisabledIf); } catch (Exception ex)
                { errors.Add($"DisabledIf 表达式语法错误 (Id: {component.Id}): {ex.Message}"); }
            }
        }
    }

    private void ValidateHandlers(DslPage dsl, List<string> errors)
    {
        foreach (var component in dsl.GetAllComponents())
        {
            if (component.Events == null) continue;
            foreach (var evt in component.Events)
            {
                if (!_validHandlers.Contains(evt.Handler) && !evt.Handler.StartsWith("custom:"))
                    errors.Add($"未预定义的 Handler: {evt.Handler} (ComponentId: {component.Id})");
            }
        }
    }
}
