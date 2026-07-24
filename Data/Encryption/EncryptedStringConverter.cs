using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace consulcore_backend.Data.Encryption;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            v => encryptionService.Encrypt(v),
            v => encryptionService.Decrypt(v))
    {
    }
}
