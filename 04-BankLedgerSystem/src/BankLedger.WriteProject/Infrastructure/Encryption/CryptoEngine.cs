using System.Security.Cryptography;

public static class CryptoEngine
{
    public static string Encrypt(string plainText, string keyBase64)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        byte[] key = Convert.FromBase64String(keyBase64);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // Generate a fresh Initialization Vector for security

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // Write the IV to the beginning of the stream so we can read it during decryption
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherTextBase64, string keyBase64)
    {
        if (string.IsNullOrEmpty(cipherTextBase64)) return cipherTextBase64;

        byte[] fullCipher = Convert.FromBase64String(cipherTextBase64);
        byte[] key = Convert.FromBase64String(keyBase64);

        using var aes = Aes.Create();
        aes.Key = key;

        // Extract the IV from the beginning of the ciphertext payload
        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
