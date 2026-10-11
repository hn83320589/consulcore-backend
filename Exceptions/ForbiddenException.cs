namespace consulcore_backend.Exceptions;

/// <summary>
/// 已登入、但這個操作不被允許（403）。訊息會回傳給前端。
/// 不要用 UnauthorizedAccessException 表達這種情況：它對應 401，前端會把使用者登出。
/// </summary>
public class ForbiddenException(string message) : Exception(message);
