// 双产出（复用 CJDSL.React/build.cjs 范式）：
//   1) dist/cjdsl-page.js —— IIFE 全局脚本，react/react-dom/client/@cj/cjdsl-react 全部打入 bundle。
//      各产品 <script src=".../cjdsl-page.js"> 直接加载即用，无需安装 React，真正框架无关。
//   2) dist/cjdsl-page.esm.js —— ESM，external react/react-dom（供打包器 import，宿主自备 React）。
// 约定：@cj/cjdsl-react 通过 alias 直接解析到其源码入口（../CJDSL.React/src/index.tsx），无需先 build React 包。
const { build } = require("esbuild");
const path = require("path");

const REACT_ENTRY = path.resolve(__dirname, "../CJDSL.React/src/index.tsx");

const reactAliasPlugin = {
  name: "cjdsl-web-component-react-alias",
  setup(b) {
    b.onResolve({ filter: /^@cj\/cjdsl-react$/ }, () => ({
      path: REACT_ENTRY,
    }));
  },
};

// 1) IIFE 全局脚本（主交付：<script> 直接加载，React 内置）
build({
  entryPoints: ["src/index.ts"],
  bundle: true,
  format: "iife",
  globalName: "CjdslWebComponent",
  platform: "browser",
  target: ["es2020"],
  plugins: [reactAliasPlugin],
  outfile: "dist/cjdsl-page.js",
  jsx: "transform",
  loader: { ".tsx": "tsx", ".ts": "ts" },
  logLevel: "info",
});

// 2) ESM（供打包器 import，宿主自备 React）
build({
  entryPoints: ["src/index.ts"],
  bundle: true,
  format: "esm",
  platform: "browser",
  target: ["es2020"],
  external: ["react", "react-dom", "react-dom/client"],
  plugins: [reactAliasPlugin],
  outfile: "dist/cjdsl-page.esm.js",
  jsx: "transform",
  loader: { ".tsx": "tsx", ".ts": "ts" },
  logLevel: "info",
});

console.log("CJDSL.WebComponent build done");
