namespace consulcore_backend.DTOs.User;

/// <summary>心理師下拉選單的一個選項：只含識別與顯示用的姓名，不含聯絡資料。</summary>
public class TherapistOptionDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
