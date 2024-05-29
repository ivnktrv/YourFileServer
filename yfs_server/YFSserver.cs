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

        Console.Clear();
        Console.WriteLine("\x1b[3J");
        Console.WriteLine($"##### IP: {net.getIP()}, ПОРТ: {port} #####\n");

        Socket socket = net.createServer(port);
        Socket connClient = socket.Accept();

        Console.WriteLine($"[i] Подключён клиент (IP: {connClient.RemoteEndPoint})");

        while (true)
        {
            byte[] getBuffLength = new byte[1];
            connClient.Receive(getBuffLength);
            byte[] buff = new byte[getBuffLength[0]];
            connClient.Receive(buff);
            string cmd = Encoding.UTF8.GetString(buff);

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
                byte[] getArrSize = new byte[1];
                connClient.Receive(getArrSize);
                byte[] getB = new byte[getArrSize[0]];
                connClient.Receive(getB);
                string getFileName = Encoding.UTF8.GetString(getB);

                io.uploadFile(connClient, getFileName, folder: setDir);
            }

            else if (cmd == "delete")
            {
                byte[] getArrSize = new byte[1];
                connClient.Receive(getArrSize);
                byte[] getDelFile = new byte[getArrSize[0]];
                connClient.Receive(getDelFile);

                File.Delete($"{setDir}{Encoding.UTF8.GetString(getDelFile)}");
            }
        
            else if (cmd == "cd")
            {
                byte[] getArrSize = new byte[1];
                connClient.Receive(getArrSize);
                byte[] getB = new byte[getArrSize[0]];
                connClient.Receive(getB);
                string getDir = Encoding.UTF8.GetString(getB);

                if (getDir == "/")
                    setDir = rootDir;
                else
                    setDir = getDir;
            }
        }
    }
}
