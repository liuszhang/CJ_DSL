// 轻量表达式求值（visibleIf / disabledIf）：
//  白名单语法：布尔/数字/字符串字面量、data.<field> 引用、==/!=/</>/<=/>=、&&/||、!、括号、includes()
//  禁止 eval/Function/对象访问（不含 . 的裸标识符一律视为 data.<identifier>）
//  任何不支持的结构 → 返回 undefined（视为不生效，由调用方决定默认值）
import { DslStore } from "./store";

type Token =
  | { t: "num"; v: number }
  | { t: "str"; v: string }
  | { t: "bool"; v: boolean }
  | { t: "ident"; v: string }
  | { t: "op"; v: string }
  | { t: "lparen" }
  | { t: "rparen" }
  | { t: "comma" }
  | { t: "eof" };

function tokenize(input: string): Token[] | null {
  const tokens: Token[] = [];
  let i = 0;
  const n = input.length;
  while (  i < n) {
    const c = input[i];
    if (/\s/.test(c)) {
      i++;
      continue;
    }
    if (c === "(") {
      tokens.push({ t: "lparen" });
      i++;
      continue;
    }
    if (c === ")") {
      tokens.push({ t: "rparen" });
      i++;
      continue;
    }
    if (c === ",") {
      tokens.push({ t: "comma" });
      i++;
      continue;
    }
    // 字符串字面量
    if (c === '"' || c === "'") {
      const quote = c;
      let j = i + 1;
      let buf = "";
      let closed = false;
      while (j < n) {
        if (input[j] === "\\" && j + 1 < n) {
          buf += input[j + 1];
          j += 2;
          continue;
        }
        if (input[j] === quote) {
          closed = true;
          break;
        }
        buf += input[j];
        j++;
      }
      if (!closed) return null;
      tokens.push({ t: "str", v: buf });
      i = j + 1;
      continue;
    }
    // 数字
    if (/[0-9.]/.test(c) && /[0-9]/.test(c)) {
      let j = i;
      while (j < n && /[0-9.]/.test(input[j])) j++;
      const num = Number(input.slice(i, j));
      if (Number.isNaN(num)) return null;
      tokens.push({ t: "num", v: num });
      i = j;
      continue;
    }
    // 运算符
    const two = input.slice(i, i + 2);
    if (["==", "!=", ">=", "<=", "&&", "||"].includes(two)) {
      tokens.push({ t: "op", v: two });
      i += 2;
      continue;
    }
    if (["!", ">", "<", "="].includes(c)) {
      tokens.push({ t: "op", v: c });
      i++;
      continue;
    }
    // 标识符（字母/数字/下划线/点）
    if (/[A-Za-z_]/.test(c)) {
      let j = i;
      while (j < n && /[A-Za-z0-9_.]/.test(input[j])) j++;
      const ident = input.slice(i, j);
      if (ident === "true" || ident === "false") {
        tokens.push({ t: "bool", v: ident === "true" });
      }  else {
        tokens.push({ t: "ident", v: ident });
      }
      i = j;
      continue;
    }
    return null;
  }
  tokens.push({ t: "eof" });
  return tokens;
}

// 递归下降解析：expr → or → and → cmp → primary
class Parser {
  constructor(private tokens: Token[], private store: DslStore) {}

  private pos = 0;
  private peek(): Token {
    return this.tokens[this.pos];
  }
  private next(): Token {
    return this.tokens[this.pos++];
  }
  private expectOp(v: string): boolean {
    const tok = this.peek();
    if (tok.t === "op" && tok.v === v) {
      this.pos++;
      return true;
    }
    return false;
  }

  parse(): boolean | undefined {
    const v = this.parseOr();
    return this.peek().t === "eof" ? v : undefined;
  }

  private parseOr(): boolean | undefined {
    const left = this.parseAnd();
    if (left === undefined) return undefined;
    while (this.expectOp("||")) {
      const right = this.parseAnd();
      if (right === undefined) return undefined;
      if (left || right) return true;
    }
    return left;
  }

  private parseAnd(): boolean | undefined {
    const left = this.parseCmp();
    if (left === undefined) return undefined;
    while (this.expectOp("&&")) {
      const right = this.parseCmp();
      if (right === undefined) return undefined;
      if (!left || !right) return false;
    }
    return left;
  }

  private parseCmp(): boolean | undefined {
    const left = this.parsePrimary();
    if (left === undefined) return undefined;
    const tok = this.peek();
    if (tok.t === "op" && ["==", "!=", ">", "<", ">=", "<="].includes(tok.v)) {
      this.pos++;
      const right = this.parsePrimary();
      if (right === undefined) return undefined;
      const l = left[0];
      const r = right[0];
      switch (tok.v) {
        case "==":
          return String(l) === String(r);
        case "!=":
          return String(l) !== String(r);
        case ">":
          return (l as number) > (r as number);
        case "<":
          return (l as number) < (r as number);
        case ">=":
          return (l as number) >= (r as number);
        case "<=":
          return (l as number) <= (r as number);
      }
    }
    return left[1];
  }

  /** primary 返回 [值, 布尔语义]；支持 ! 前缀与 includes() 调用 */
  private parsePrimary(): [unknown, boolean | undefined] | undefined {
    const tok = this.  peek();
    if (tok.t === "op" && tok.v === "!") {
      this.pos++;
      const inner = this.parsePrimary();
      if (inner === undefined) return undefined;
      return [inner[0], inner[1] === undefined ? undefined : !inner[1]];
    }
    if (tok.t === "lparen") {
      this.pos++;
      const v = this.parseOr();
      if (v === undefined) return undefined;
      if (this.peek().t !== "rparen") return undefined;
      this.pos++;
      return [v, v];
    }
    if (tok.t === "num") {
      this.pos++;
      return [tok.v, !!tok.v];
    }
    if (tok.t === "str") {
      this.pos++;
      return [tok.v, tok.v !== ""];
    }
    if (tok.t === "bool") {
      this.pos++;
      return [tok.v, tok.v];
    }
    if (tok.t === "ident") {
      this.pos++;
      // includes() 调用
      if (this.peek().t === "lparen") {
        this.pos++;
        const arg = this.parsePrimary();
        if (arg === undefined) return undefined;
        if (this.peek().t !== "rparen") return undefined;
        this.pos++;
        const haystack = this.lookupIdent(tok.v);
        const needle = String(arg[0]);
        if (Array.isArray(haystack)) return [true, haystack.some((x) => String(x) === needle)];
        if (typeof haystack === "string") return [true, haystack.includes(needle)];
        return [false, false];
      }
      return [this.lookupIdent(tok.v), undefined];
    }
    return undefined;
  }

  private lookupIdent(ident: string): unknown {
    const key = ident.startsWith("data.") ? ident.slice(5) : ident;
    return this.store.get(`data.${key}`);
  }
}

/**
 * 求值表达式。返回：
 *  - true/false：白名单语法内可判定
 *  - undefined：语法不支持（由调用方按"不生效"处理）
 */
export function evalDslExpr(expr: string | undefined, store: DslStore): boolean | undefined {
  if (!expr || expr.trim() === "") return undefined;
  const tokens = tokenize(expr);
  if (!tokens) return undefined;
  const parser = new Parser(tokens, store);
  try {
    return parser.parse();
  } catch {
    return undefined;
  }
}
