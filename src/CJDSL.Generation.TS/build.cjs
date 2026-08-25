// 双构建：lib/cjs（ESM，供打包器/宿主 import）+ lib/cjs/index.js
// 生成库无 React 依赖，纯 TS；保留与 CJDSL.React 一致的双产物范式以便统一。
const { build } = require("esbuild");
const path = require("path");

const PKG = "@cj/cjdsl-generation-ts";

// 包自身 exports 指向构建产物（构建时还不存在），用 alias 解析到源码入口避免自引用失败
const selfAliasPlugin = {
  name: "cjdsl-generation-ts-self-alias",
  setup(b) {
    b.onResolve({ filter: /^@cj\/cjdsl-generation-ts$/ }, () => ({
      path: path.resolve(__dirname, "src/index.ts"),
    }));
  },
};

build({
  entryPoints: ["src/index.ts"],
  bundle: true,
  format: "esm",
  platform: "browser",
  plugins: [selfAliasPlugin],
  outfile: "lib/cjs/index.js",
  logLevel: "info",
});

console.log("CJDSL.Generation.TS build done");
