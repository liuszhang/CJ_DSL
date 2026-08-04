using CJCore.Framework.Abstractions;

namespace CJDSL.Web.Services;

/// <summary>
/// CJDSL 模块 — 实现 IModule 以接入 CJCore 框架的程序集发现。
/// </summary>
public class CJDSLModule : ModuleBase
{
    public override string Name => "CJDSL";
}
