using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace Infrastructure.Registration;

internal sealed class VerificationCodes(IOptions<JwtOptions> options)
{
    private readonly byte[] key = HKDF.DeriveKey(HashAlgorithmName.SHA256,
        Encoding.UTF8.GetBytes(options.Value.SigningKey), 32, info: "curator/registration-code/v1"u8.ToArray());

    public Guid NewNonce(Guid previous)
    {
        var previousCode = Get(previous);
        Guid nonce;
        do
        {
            nonce = Guid.NewGuid();
        }
        while (Get(nonce) == previousCode);
        return nonce;
    }

    public string Get(Guid nonce)
    {
        var digest = HMACSHA256.HashData(key, nonce.ToByteArray());
        var number = BinaryPrimitives.ReadUInt64BigEndian(digest) % 1_000_000;
        return number.ToString("D6", CultureInfo.InvariantCulture);
    }

    public bool Matches(Guid nonce, string code) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Get(nonce)), Encoding.ASCII.GetBytes(code));
}
