namespace consulcore_backend.Exceptions;

/// <summary>
/// 違反業務規則（400），訊息會回傳給前端顯示。
/// 業務程式請用這個，不要用 InvalidOperationException：後者也會由 EF / 框架丟出，
/// 其訊息含內部細節，不可外洩給前端。
/// </summary>
public class BusinessRuleException(string message) : Exception(message);
