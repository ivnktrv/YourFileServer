using System.Security.Cryptography;
using System.Net.Sockets;
using System.Text;

namespace yfs_security;

public class YFSsec
{
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

    private string genSHA256(string s)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

        return hash.ToLower();
    }

    public string checksumFileSHA256(string path)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream fs = File.OpenRead(path);
        byte[] hashBytes = sha256.ComputeHash(fs);
        string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);

        return hash.ToLower();
    }
}
