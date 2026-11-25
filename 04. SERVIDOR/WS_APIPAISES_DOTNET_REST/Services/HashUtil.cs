using System.Security.Cryptography;
using System.Text;

namespace WS_APIPAISES_DOTNET_REST.Services;

/// <summary>
/// SHA-256 en hexadecimal en minúsculas (64 caracteres). Es el mismo valor que produce
/// LOWER(CONVERT(char(64), HASHBYTES('SHA2_256', texto), 2)) en SQL Server, con el que se
/// guardan las claves de los usuarios de prueba.
/// </summary>
public class HashUtil
{
    public string Sha256(string texto)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Compara el hash de la clave recibida con el guardado, sin distinguir mayúsculas del hex.</summary>
    public bool Coincide(string? clave, string? hashGuardado)
    {
        if (clave == null || hashGuardado == null)
        {
            return false;
        }
        byte[] calculado = Encoding.ASCII.GetBytes(Sha256(clave));
        byte[] guardado = Encoding.ASCII.GetBytes(hashGuardado.Trim().ToLowerInvariant());
        // Comparación en tiempo constante
        return CryptographicOperations.FixedTimeEquals(calculado, guardado);
    }
}
