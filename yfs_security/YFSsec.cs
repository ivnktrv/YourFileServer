using System.Security.Cryptography;
using System.Net.Sockets;
using System.Text;
using yfs_io;

namespace yfs_security;

public class YFSsec
{
    YFSio io = new();

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
        string authLogin = File.ReadAllLines("AUTH")[0];
        string authPasswordHash = File.ReadAllLines("AUTH")[1];

        if (login == authLogin && passHash == authPasswordHash)
        {
            return true;
        }
        else
        {
            return false;
        }
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
        io.clearTerminal();
        Console.WriteLine("##### СОЗДАНИЕ ФАЙЛА АВТОРИЗАЦИИ #####\n");
        Console.Write("Придумайте логин: ");
        string login = Console.ReadLine();
        Console.Write("Придумайте пароль: ");
        string pass = Console.ReadLine();

        string passHash = genSHA256(pass);
        using FileStream fs = new("AUTH", FileMode.Create, FileAccess.Write);
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
}
