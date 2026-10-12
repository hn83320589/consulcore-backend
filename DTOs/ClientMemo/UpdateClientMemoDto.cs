using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ClientMemo;

public class UpdateClientMemoDto
{
    [ConstantValues(typeof(MemoCategory))]
    public string? Category { get; set; }
    public string? Content { get; set; }
}
