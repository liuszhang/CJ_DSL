// 双构建：lib/cjs（供 TS/打包器按需打包）+ lib/client.js（包进 DSH __ModuleLoader__.load）
// 沿用 DA.DSHPlug.CJDSL/build.mjs 范式：
//   lib/cjs：TS 源码编译（ESM，供 React/打包器 import）
//   lib/client.js：浏览器 CJS，external react/@deepseek-ai/*，banner/footer 包裹
const { build } = require("esbuild");
const path = require("path");

const PKG = "@cj/cjdsl-react";

// client-entry.tsx 通过包名自引用本包公共 API；包自身 exports 指向构建产物
// （构建时还不存在），用 alias 直接解析到源码入口，避免自引用解析失败。
const selfAliasPlugin = {
  name: "cjdsl-react-self-alias",
  setup(b) {
    b.onResolve({ filter: /^@cj\/cjdsl-react$/ }, () => ({
      path: path.resolve(__dirname, "src/index.tsx"),
    }));
  },
};

// 1) 标准 TS 编译产物（供打包器/宿主 import @cj/cjdsl-react）
build({
  entryPoints: ["src/index.tsx"],
  bundle: true,
  format: "esm",
  platform: "browser",
  external: ["react", "react/*", "@deepseek-ai/*"],
  outfile: "lib/cjs/index.js",
  jsx: "transform",
  logLevel: "info",
});

// 2) DSH 兼容产物：src/client-entry.tsx → lib/client.js（CJS，包进 window.__ModuleLoader__.load）
build({
  entryPoints: ["src/client-entry.tsx"],
  bundle: true,
  format: "cjs",
  platform: "browser",
  external: ["react", "react/*", "@deepseek-ai/*"],
  plugins: [selfAliasPlugin],
  outfile: "lib/client.js",
  jsx: "transform",
  banner: {
    js: `window.__ModuleLoader__.load({ id: "${PKG}", factory: (require) => { var module = { exports: {} }; var exports = module.exports;`,
  },
  footer: {
    js: `return module.exports; } });`,
  },
  logLevel: "info",
});

console.log("CJDSL.React build done");
