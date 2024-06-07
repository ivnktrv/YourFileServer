using System.Net.Sockets;
using System.Text;
using System.Net;

namespace yfs_net;

public class YFSnet
{
    public string getIP()
    {
        var getIPs = new List<string>();
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                getIPs.Add(ip.ToString());
            }
        }
        string[] ips = getIPs.ToArray();

        Console.WriteLine("##### СПИСОК IP #####\n");
        for (int i = 0; i < ips.Length; i++)
        {
            Console.WriteLine($"[{i}] {ips[i]}");
        }
        Console.Write("\nКакой IP выбрать?: ");
        ConsoleKeyInfo key = Console.ReadKey();
        Console.Clear();
        Console.WriteLine("\x1b[3J");
        Console.WriteLine($"[i] Выбран IP: {ips[int.Parse(key.KeyChar.ToString())]}");

        return ips[int.Parse(key.KeyChar.ToString())];
    }

    public Socket createServer(string ip, int port)
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        Socket __socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        __socket.Bind(ipPoint);
        __socket.Listen();

        return __socket;
    }

    public Socket createClient(string ip, int port)
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        Socket __socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        __socket.Connect(ipPoint);

        return __socket;
    }

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
