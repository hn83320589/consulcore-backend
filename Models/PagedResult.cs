using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Models;

/// <summary>伺服器分頁的一頁資料；TotalCount 為符合篩選條件的總筆數</summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public static class PagedResultExtensions
{
    /// <summary>
    /// 先計算篩選後的總筆數，再取該頁並投影。query 須已排序（分頁需要穩定順序）。
    /// 投影放在 Skip / Take 之後，總筆數的 COUNT 不受投影影響。
    /// </summary>
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IOrderedQueryable<TSource> query, int page, int pageSize, Expression<Func<TSource, TResult>> selector)
    {
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(selector).ToListAsync();
        return new PagedResult<TResult> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }
}
