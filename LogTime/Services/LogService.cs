using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace LogTime.Services;

public class LogService : ILogService
{
    // Encryption key (keep this secret)
    private static string encryptionKey = "SynergiesCorpKey";
    // Initialization Vector (IV)
    private static string customIV = "SCSCSCSCSCSCSCSC";

    public void Log(LogEntry logEntry)
    {
        var filePath = $"C:\\LogTimeLogs\\{logEntry.UserId}.log";
        var header = "Version         | Date                      | Class Name                | Method Name                                        | Log Message";
        var separator = new string('-', logEntry.ToString().Length);
        byte[] bytesToEncode;
        string base64Text;
        var directory = System.IO.Path.GetDirectoryName(filePath);
        var fileExists = File.Exists(filePath);

        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(filePath, append: true);

        if (!fileExists)
        {
            bytesToEncode = EncryptText(header, encryptionKey, customIV);
            base64Text = Convert.ToBase64String(bytesToEncode);
            writer.WriteLine(base64Text);
            writer.WriteLine(separator);
        }

        logEntry.Version = GlobalData.AppVersion;

        bytesToEncode = EncryptText(logEntry.ToString(), encryptionKey, customIV);
        base64Text = Convert.ToBase64String(bytesToEncode);

        writer.WriteLine(base64Text);
        writer.WriteLine(separator);
    }

    private static byte[] EncryptText(string plainText, string key, string iv)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(key);
        aesAlg.IV = Encoding.UTF8.GetBytes(iv);

        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msEncrypt = new();
        using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            using StreamWriter swEncrypt = new(csEncrypt);
            swEncrypt.Write(plainText);
        }

        return msEncrypt.ToArray();
    }

    private static string DecryptText(byte[] cipherText, string key, string iv)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(key);
        aesAlg.IV = Encoding.UTF8.GetBytes(iv);

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msDecrypt = new(cipherText);
        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    public void ShowLog(string userId)
    {
        var logName = $"{userId}.log";
        string filePath = $"C:\\LogTimeLogs/{logName}";

        if (!File.Exists(filePath))
        {
            DialogBox.Show($"El archivo ({logName}) no existe.", "LogTime - Archivo No Existe", DialogBoxButton.OK, AlertType.Error);
            return;
        }

        var logLines = File.ReadAllLines(filePath).Skip(1).Where(line => !line.StartsWith('-'));
        ObservableCollection<LogEntry> logs = new(logLines.Select(ParseLogString));

        var fileLogWindow = new FileLogWindow
        {
            Title = logName,
            DataContext = new { Logs = logs }
        };

        fileLogWindow.Show();
    }

    private LogEntry ParseLogString(string logString)
    {
        string[] parts;
        try
        {
            byte[] lineBytes = Convert.FromBase64String(logString);
            string lineDecoded = DecryptText(lineBytes, encryptionKey, customIV);

            parts = lineDecoded.Split('|').Select(p => p.Trim()).ToArray();

            return new LogEntry
            {
                Version = parts[0],
                Date = parts[1],
                ClassName = parts[2],
                MethodName = parts[3],
                LogMessage = parts[4]
            };

        }
        catch (Exception)
        {
            throw;
        }
    }
}
