namespace CJDSL.Domain.Interfaces;

/// <summary>
/// 业务数据记录（通用宽表模型：任意元模型对象的数据以 JSON 文本存储）
/// </summary>
public class BusinessDataRecord
{
    /// <summary>记录唯一标识</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>元模型对象编码（如 repair_order、equipment）</summary>
    public string ObjectCode { get; set; } = string.Empty;

    /// <summary>状态：draft（草稿）| submitted（已提交）</summary>
    public string Status { get; set; } = "draft";

    /// <summary>业务数据 JSON 文本</summary>
    public string JsonData { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 分页查询结果
/// </summary>
public class BusinessDataPage
{
    public IReadOnlyList<BusinessDataRecord> Items { get; set; } = Array.Empty<BusinessDataRecord>();
    public int Total { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// 通用业务数据服务 - 元模型驱动的动态 Schema 存取。
/// 数据以 JSON 文本形式存储，端点根据元模型对象编码动态适配，无需强类型实体。
/// </summary>
public interface IBusinessDataService
{
    /// <summary>保存业务数据（jsonData 中含 id 且已存在则更新，否则新建，状态为 draft）</summary>
    Task<BusinessDataRecord> SaveAsync(string objectCode, string jsonData, CancellationToken ct = default);

    /// <summary>提交业务数据（保存并将状态置为 submitted）</summary>
    Task<BusinessDataRecord> SubmitAsync(string objectCode, string jsonData, CancellationToken ct = default);

    /// <summary>分页查询指定对象的数据列表</summary>
    Task<BusinessDataPage> ListAsync(string objectCode, int pageIndex = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>按 Id 获取单条记录</summary>
    Task<BusinessDataRecord?> GetAsync(string objectCode, string id, CancellationToken ct = default);

    /// <summary>按 Id 删除记录</summary>
    Task<bool> DeleteAsync(string objectCode, string id, CancellationToken ct = default);
}
