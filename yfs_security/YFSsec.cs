using System.Security.Cryptography;
using System.Net.Sockets;
using System.Text;
using yfs_keygen;

namespace yfs_security;

public class YFSsec
{
    private const byte BYTE255 = 0b11111111;


    public bool checkAuthData(Socket __socket)
    {
        byte[] getLoginLength = new byte[1];
        __socket.Receive(getLoginLength);

        byte[] getLogin = new byte[getLoginLength[0]];
        __socket.Receive(getLogin);

        byte[] getPassHash = new byte[64];
        __socket.Receive(getPassHash);

        string login = Encoding.UTF8.GetString(getLogin);
        string passHash = Encoding.UTF8.GetString(getPassHash);
        string authLogin = File.ReadAllLines($"yfs_{Environment.MachineName}.auth")[0];
        string authPasswordHash = File.ReadAllLines($"yfs_{Environment.MachineName}.auth")[1];

        Console.WriteLine($"[{DateTime.Now}] [...] Проверяю данные для входа");
        if (login == authLogin && passHash == authPasswordHash)
            return true;
        else
            return false;
    }

    public void sendAuthData(Socket __socket)
    {
        Console.Write("\nЛогин: ");
        string login = Console.ReadLine();
        Console.Write("Пароль: ");
        string pass = Console.ReadLine();
        string passHash = genSHA256(pass);

        byte[] getLoginLength = { (byte)login.Length };
        byte[] sendLogin = Encoding.UTF8.GetBytes(login);
        byte[] sendPassHash = Encoding.UTF8.GetBytes(passHash);

        __socket.Send(getLoginLength);
        __socket.Send(sendLogin);
        __socket.Send(sendPassHash);
    }

    public void createAuthFile()
    {
        Console.Clear();
        Console.WriteLine("\x1b[3J");
        Console.WriteLine("##### СОЗДАНИЕ ФАЙЛА АВТОРИЗАЦИИ #####\n");
        Console.Write("Придумайте логин: ");
        string login = Console.ReadLine();
        Console.Write("Придумайте пароль: ");
        string pass = Console.ReadLine();

        string passHash = genSHA256(pass);
        using FileStream fs = new($"yfs_{Environment.MachineName}.auth", FileMode.Create, FileAccess.Write);
        fs.Write(Encoding.UTF8.GetBytes($"""
            {login}
            {passHash}
            """));
    }

    public string checksumFileSHA256(string path)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream fs = File.OpenRead(path);
        byte[] hashBytes = sha256.ComputeHash(fs);
        string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

        return hash.ToLower();
    }

    private string genSHA256(string s)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

        return hash.ToLower();
    }

    public string encryptFile(string openFile)
    {
        using BinaryReader readFile = new(File.Open(openFile, FileMode.Open));
        using BinaryWriter writeFile = new(File.Open(openFile+".enc", FileMode.Create));

        string key = new YFSkeygen()._keygen();
        byte[] _keyBytes = new YFSkeygen()._keyBytes(key);

        Console.WriteLine("[...] Файл шифруется перед отправкой. Подождите");
        while (readFile.BaseStream.Position != readFile.BaseStream.Length)
        {
            byte wb = readFile.ReadByte();
            foreach (byte b in _keyBytes)
            {
                wb ^= b;
                wb = (byte)(BYTE255 - wb);
            }
            writeFile.Write(wb);
        }
        Console.WriteLine("\n[+] Сделано");

        return key;
    }

    public void decryptFile(string openFile, string key)
    {
        using BinaryReader readFile = new(File.Open(openFile, FileMode.Open));
        using BinaryWriter writeFile = new(File.Open(openFile.Remove(openFile.Length-4), FileMode.Create));

        Console.WriteLine("[...] Файл расшифровыается");
        while (readFile.BaseStream.Position != readFile.BaseStream.Length)
        {
            byte wb = readFile.ReadByte();
            foreach (byte b in key.Reverse())
            {
                wb ^= b;
                wb = (byte)(BYTE255 - wb);
            }
            writeFile.Write(wb);
        }
        Console.WriteLine("[+] Сделано");
    }

}
