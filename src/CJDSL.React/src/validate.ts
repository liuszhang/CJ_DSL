// 基础校验（v1：required/minLength/maxLength/regex/min/max）
export interface ValidationRule {
  type: string;
  message?: string;
  pattern?: string;
  min?: number;
  max?: number;
  expression?: string;
}

export interface FieldValidationResult {
  valid: boolean;
  errors: string[];
}

function defaultMessage(rule: ValidationRule): string {
  switch (rule.type) {
    case "required":
      return "该字段为必填项";
    case "minLength":
      return `长度不能少于 ${rule.min} 个字符`;
    case "maxLength":
      return `长度不能超过 ${rule.max} 个字符`;
    case "regex":
      return "格式不正确";
    case "min":
      return `数值不能小于 ${rule.min}`;
    case "max":
      return `数值不能大于 ${rule.max}`;
    default:
      return "校验未通过";
  }
}

export function validateField(value: unknown, rules: ValidationRule[] | undefined): FieldValidationResult {
  const errors: string[] = [];
  if (!rules || rules.length === 0) return { valid: true,  errors };

  for (const rule of rules) {
    const msg = rule.message || defaultMessage(rule);
    switch (rule.type) {
      case "required": {
        const v = value as string | undefined;
        if (v === undefined || v === null || String(v).trim() === "") errors.push(msg);
        break;
      }
      case "minLength": {
        const v = value as string | undefined;
        if (v !== undefined && v !== null && String(v).length < (rule.min ?? 0)) errors.push(msg);
        break;
      }
      case "maxLength": {
        const v = value as string | undefined;
        if (v !== undefined && v !== null && String(v).length > (rule.max ?? 0)) errors.push(msg);
        break;
      }
      case "regex": {
        const v = value as string | undefined;
        if (v !== undefined && v !== null && String(v).trim() !== "") {
          try {
            const re = new RegExp(rule.pattern ?? "");
            if (!re.test(String(v))) errors.push(msg);
          } catch {
            errors.push("正则表达式无效");
          }
        }
        break;
      }
      case "min": {
        const n = Number(value);
        if (!Number.isNaN(n) && n < (rule.min ?? 0)) errors.push(msg);
        break;
      }
      case "max": {
        const n = Number(value);
        if (!Number.isNaN(n) && n > (rule.max ?? 0)) errors.push(msg);
        break;
      }
    }
  }
  return { valid: errors.length === 0, errors };
}
