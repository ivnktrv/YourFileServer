using System.Net.Sockets;
using System.Net;

namespace yfs_net;

public class YFSnet
{
    public string getIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("Отсутствуют адаптары");
    }

    public Socket createServer(int port)
    {
        IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(getIP()), port);
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
}
