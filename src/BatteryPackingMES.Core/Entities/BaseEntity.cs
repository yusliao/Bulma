using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 实体基类
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    [Display(Name = "主键ID")]
    public long Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "创建时间")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 创建人ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "创建人ID")]
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "更新时间")]
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 更新人ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "更新人ID")]
    public long? UpdatedBy { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否删除")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 版本号（乐观锁）
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "版本号")]
    public int Version { get; set; } = 1;
} 