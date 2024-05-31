using System.Net.Sockets;
using System.Text;
using yfs_net;
using yfs_io;

namespace yfs_server;

public class YFSserver
{
    YFSnet net = new();
    YFSio  io  = new();
    
    private string rootDir;
    private string setDir;
    private string setIP;

    public void run()
    {
        io.clearTerminal();
        Console.Write("Какую папку выделить для сервера?: ");
        rootDir = Console.ReadLine();
        Console.Write("Укажите порт: ");
        int port = int.Parse(Console.ReadLine());

        setDir = rootDir;
        io.clearTerminal();

        setIP = net.getIP();
        Console.WriteLine($"[i] Ожидаю подключения\n");
        
        while (true)
        {
            Socket socket = net.createServer(setIP, port);
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
