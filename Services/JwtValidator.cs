using System;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using SalesBotApi.Models;
using Newtonsoft.Json;

public class JwtValidator
{
    public static JwtPayload ValidateAndDecodeToken(string token, string secret)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null; // Invalid token format
        }

        var header = parts[0];
        var payload = parts[1];
        var signature = parts[2];

        var computedSignature = ComputeJwtSignature(header, payload, secret);
        if (signature != computedSignature)
        {
            return null; // Signature validation failed
        }

        return DecodePayload(payload);
    }

    private static string ComputeJwtSignature(string header, string payload, string secret)
    {
        var signature = $"{header}.{payload}";
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signature));
            return Convert.ToBase64String(signatureBytes);
        }
    }

    private static JwtPayload DecodePayload(string encodedPayload)
    {
        var jsonBytes = Convert.FromBase64String(encodedPayload);
        string payloadStr = Encoding.UTF8.GetString(jsonBytes);
        JwtPayload deserializedPayload = JsonConvert.DeserializeObject<JwtPayload>(payloadStr);
        return deserializedPayload;
    }
}
