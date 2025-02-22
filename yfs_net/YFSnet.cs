//
// Класс YFSnet предназначен для работы с сетевыми соединениями.
//
// Основные методы:
//
// • getIP: Получает список IP адресов текущего хоста и позволяет выбрать один из них.
// • createServer: Создает серверный сокет, привязанный к указанному IP-адресу и порту.
// • createClient: Создает клиентский сокет и подключается к указанному IP-адресу и порту.
// • sendDataAsync и sendData: Отправляют данные через сокет асинхронно и синхронно соответственно.
// • getDataAsync и getData: Получают данные из сокета асинхронно и синхронно соответственно.
//

using System.Net.Sockets;
using System.Text;
using System.Net;

namespace yfs_net;

public class YFSnet
{
    /// <summary>
    /// Получение списка IP адресов текущего хоста.
    /// </summary>
    /// <returns></returns>
    public string getIP()
    {
        List<string> getIPs = new List<string>();
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (IPAddress ip in host.AddressList)
        {
            getIPs.Add(ip.ToString());
        }
        string[] ips = getIPs.ToArray();

        Console.WriteLine("##### СПИСОК IP #####\n");
        for (int i = 0; i < ips.Length; i++)
        {
            Console.WriteLine($"[{i}] {ips[i]}");
        }
        Console.Write("\nКакой IP выбрать?: ");
        ConsoleKeyInfo key = Console.ReadKey();

        return ips[int.Parse(key.KeyChar.ToString())];
    }

    /// <summary>
    /// Создание серверного сокета, привязанный к указанному IP-адресу и порту.
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public Socket createServer(string ip, int port)
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        Socket __socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        __socket.Bind(ipPoint);
        __socket.Listen();

        return __socket;
    }

    /// <summary>
    /// Создание клиентского сокета и подключение к указанному IP-адресу и порту.
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public Socket createClient(string ip, int port)
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        Socket __socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        __socket.Connect(ipPoint);

        return __socket;
    }

    /// <summary>
    /// Отправка данных через сокет асинхронно.
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public async Task sendDataAsync(Socket __socket, string data)
    {
        try
        {
            byte[] buff = Encoding.UTF8.GetBytes(data);
            byte[] buffLength = { (byte)buff.Length };
            await __socket.SendAsync(buffLength);
            await __socket.SendAsync(buff);
        }
        catch (SocketException)
        {
            Console.WriteLine("\nСервер отключился");
        }
    }

    /// <summary>
    /// Отправка данных через сокет синхронно.
    /// </summary>
    /// <param name="__socket"></param>
    /// <param name="data"></param>
    public void sendData(Socket __socket, string data)
    {
        try
        {
            byte[] buff = Encoding.UTF8.GetBytes(data);
            byte[] buffLength = { (byte)buff.Length };
            __socket.Send(buffLength);
            __socket.Send(buff);
        }
        catch (SocketException)
        {
            Console.WriteLine("\nСервер отключился");
        }
    }

    /// <summary>
    /// Получение данных из сокета асинхронно.
    /// </summary>
    /// <param name="__socket"></param>
    /// <returns></returns>
    public async Task<byte[]> getDataAsync(Socket __socket)
    {
        try
        {
            byte[] getBuffLength = new byte[1];
            await __socket.ReceiveAsync(getBuffLength);
            byte[] buff = new byte[getBuffLength[0]];
            await __socket.ReceiveAsync(buff);

            return buff;
        }
        catch (SocketException)
        {
            return Encoding.UTF8.GetBytes("closeconn");
        }
    }

    /// <summary>
    /// Получение данных из сокета синхронно.
    /// </summary>
    /// <param name="__socket"></param>
    /// <returns></returns>
    public byte[] getData(Socket __socket)
    {
        try
        {
            byte[] getBuffLength = new byte[1];
            __socket.Receive(getBuffLength);
            byte[] buff = new byte[getBuffLength[0]];
            __socket.Receive(buff);

            return buff;
        }
        catch (SocketException)
        {
            return Encoding.UTF8.GetBytes("closeconn");
        }
    }
}
