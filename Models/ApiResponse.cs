namespace consulcore_backend.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string msg = "操作成功")
        => new() { Success = true, Message = msg, Data = data };

    public static ApiResponse<T> Fail(string msg, List<string>? errors = null)
        => new() { Success = false, Message = msg, Errors = errors };
}
