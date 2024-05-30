using System.Net.Sockets;
using yfs_net;
using yfs_io;
using System.Text;

namespace yfs_server;

public class YFSserver
{
    YFSnet net = new();
    YFSio io = new();
    
    private string rootDir;
    private string setDir;

    public void run()
    {
        Console.Write("\nКакую папку выделить для сервера?: ");
        rootDir = Console.ReadLine();
        Console.Write("Укажите порт: ");
        int port = int.Parse(Console.ReadLine());

        setDir = rootDir;

        io.clearTerminal();

        Console.WriteLine($"##### IP: {net.getIP()}, ПОРТ: {port} #####\n");

        while (true)
        {
            Socket socket = net.createServer(port);
            Socket connClient = socket.Accept();

            Console.WriteLine($"[i] Подключён клиент (IP: {connClient.RemoteEndPoint})");

            while (true)
            {
                byte[] getCommand = net.getData(connClient);
                string cmd = Encoding.UTF8.GetString(getCommand);

                if (cmd == "list")
                {
                    string[] getDirsAndFiles = io.getFiles(setDir);
                    foreach (string items in getDirsAndFiles)
                    {
                        byte[] b = Encoding.UTF8.GetBytes(items);
                        connClient.Send(b);
                    }
                }

                else if (cmd == "upload")
                {
                    io.downloadFile(connClient, setDir);
                }

                else if (cmd == "download")
                {
                    byte[] getFileName_bytes = net.getData(connClient);
                    string getFileName = Encoding.UTF8.GetString(getFileName_bytes);

                    try
                    {
                        io.uploadFile(connClient, getFileName, folder: setDir);
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine($"[i] Клиент {connClient.RemoteEndPoint} отключился. Жду нового клиента");
                        connClient.Close();
                        socket.Close();
                        break;
                    }
                }

                else if (cmd == "delete")
                {
                    byte[] getDelFile = net.getData(connClient);

                    File.Delete($"{setDir}/{Encoding.UTF8.GetString(getDelFile)}");
                }

                else if (cmd == "cd")
                {
                    byte[] getDir_bytes = net.getData(connClient);
                    string getDir = Encoding.UTF8.GetString(getDir_bytes);

                    if (getDir == "/")
                        setDir = rootDir;
                    else
                        setDir += "/" + getDir;
                }

                else if (cmd == "closeconn")
                {
                    Console.WriteLine($"[i] Клиент {connClient.RemoteEndPoint} отключился. Жду нового клиента");
                    connClient.Close();
                    socket.Close();
                    break;
                }
            }
        }
    }
}
