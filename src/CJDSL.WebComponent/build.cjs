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

// 修复双 React（2026-08-28）：WebComponent 自身源码 import react 解析到本项目的 node_modules/react，
// 而 alias 的 CJDSL.React 源码（../CJDSL.React/src/*，DslRenderer 等）import react 会解析到
// ../CJDSL.React/node_modules/react（嵌套独立副本）——esbuild 把两份 react 都打进 bundle，形成两个
// React 实例。react-dom 的 renderWithHooks 把 hooks dispatcher 设到副本①，DslRenderer 的 useState
// 读副本② 的 dispatcher（null）→ 抛 "Cannot read properties of null (reading 'useState')" →
// <cjdsl-page> 渲染空白（所有 DSL 渲染受影响）。
// 这里把 react 族导入统一钉到本项目的 node_modules，保证 bundle 内单实例。
const pinReactPlugin = {
  name: "cjdsl-pin-react",
  setup(b) {
    // 与 React 共享内部状态的模块必须单实例；子路径（react/jsx-runtime、react-dom/client）随根钉住。
    // require.resolve(_, { paths: [__dirname] }) 解析到本项目 node_modules 的真实入口文件，
    // 避免 esbuild 把 pin 后的目录路径当文件读（会报 "Cannot read file node_modules/react"）。
    b.onResolve({ filter: /^(react|react-dom|scheduler)(\/.*)?$/ }, (args) => ({
      path: require.resolve(args.path, { paths: [__dirname] }),
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
  plugins: [reactAliasPlugin, pinReactPlugin],
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
