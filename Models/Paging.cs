namespace consulcore_backend.Models;

/// <summary>清單分頁參數的上下限（以 [Range] 套在 query 參數上，超出範圍回 400）</summary>
public static class Paging
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;
}
