//
// Класс YFSsec предназначен для обеспечения безопасности в приложении.
//
// Основные методы:
//
// • checkAuthData: Проверяет данные для аутентификации клиента.
// • sendAuthData: Отправляет данные для аутентификации на сервер.
// • createAuthFile: Создает файл с данными для аутентификации.
// • checksumFileSHA256: Вычисляет SHA-256 контрольную сумму файла.
// • encryptFile: Шифрует файл с использованием случайного ключа.
// • decryptFile: Расшифровывает файл с использованием заданного ключа.
// • HideIP: Скрывает часть IP-адреса для анонимности.
//

using System.Security.Cryptography;
using System.Net.Sockets;
using System.Text;
using yfs_keygen;

using System.Net;

namespace yfs_security;

public class YFSsec
{
    private const byte BYTE255 = 0b11111111;

    List<IPAddress> bannedIPs = [];

    /// <summary>
    /// Проверка данных для аутентификации клиента.
    /// </summary>
    /// <param name="__socket"></param>
    /// <returns></returns>
    public bool checkAuthData(Socket __socket)
    {
        IPAddress clientAddress = ((IPEndPoint)__socket.RemoteEndPoint).Address;
        if (bannedIPs.Contains(clientAddress))
            return false;

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

        // Проверка на попытку подключения по HTTP
        if (login.Contains("HTTP"))
        {
            bannedIPs.Add(clientAddress);
            Console.WriteLine($"[{DateTime.Now}] [!] Была попытка подключения по HTTP. Клиент ({HideIP(clientAddress)}***) забанен на 10 минут.");
            Task.Run(() =>
            {
                Thread.Sleep(600000);
                bannedIPs.Remove(clientAddress);
                Console.WriteLine($"[{DateTime.Now}] [i] Клиент {HideIP(clientAddress)} разбанен");
            });
            return false;
        }
        if (login == authLogin && passHash == authPasswordHash)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Отправка данных аутентификации на сервер.
    /// </summary>
    /// <param name="__socket"></param>
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

    /// <summary>
    /// Создание файла аутентификации.
    /// </summary>
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

    /// <summary>
    /// Вычисление SHA-256 контрольной суммы файла.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string checksumFileSHA256(string path)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream fs = File.OpenRead(path);
        byte[] hashBytes = sha256.ComputeHash(fs);
        string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

        return hash.ToLower();
    }

    /// <summary>
    /// Генерация SHA-256 хэша.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private string genSHA256(string s)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

        return hash.ToLower();
    }

    /// <summary>
    /// Шифрование файла.
    /// </summary>
    /// <param name="openFile"></param>
    /// <returns></returns>
    public string encryptFile(string openFile)
    {
        using BinaryReader readFile = new(File.Open(openFile, FileMode.Open));
        using BinaryWriter writeFile = new(File.Open(openFile+".enc", FileMode.Create));

        string key = new YFSkeygen()._keygen();
        byte[] _keyBytes = new YFSkeygen()._keyBytes(key);

        Console.WriteLine("\n[...] Файл шифруется перед отправкой. Подождите");
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

    /// <summary>
    /// Расшифровка файла.
    /// </summary>
    /// <param name="openFile"></param>
    /// <param name="key"></param>
    public void decryptFile(string openFile, string key)
    {
        using BinaryReader readFile = new(File.Open(openFile, FileMode.Open));
        using BinaryWriter writeFile = new(File.Open(openFile.Remove(openFile.Length-4), FileMode.Create));

        Console.WriteLine("[...] Файл расшифровывается");
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

    /// <summary>
    /// Скрытие части IP-адреса для анонимности.
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public string HideIP(IPAddress ip)
    {
        string getIP = ip.ToString();
        return getIP.Length switch
        {
            7 => getIP[..^4],
            8 => getIP[..^5],
            9 => getIP[..^6],
            10 => getIP[..^6],
            12 => getIP[..^8],
            13 => getIP[..^8],
            14 => getIP[..^9],
            15 => getIP[..^9],
            _ => getIP[..^6],
        };
    }
}
