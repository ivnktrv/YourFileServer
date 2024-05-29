using System.Net.Sockets;
using System.Text;
using yfs_io;
using yfs_net;

namespace yfs_client;

public class YFSclient
{
    YFSio io = new();
    YFSnet net = new();

    public void run()
    {
        Console.Write("\nУкажите ip: ");
        string ip = Console.ReadLine();
        Console.Write("\nУкажите порт: ");
        int port = int.Parse(Console.ReadLine());

        Socket socket = net.createClient(ip, port);

        while (true)
        {
            Console.Clear();
            Console.WriteLine("\x1b[3J");

            Console.Write("""
                1 - скачать файл
                2 - загрузить файл
                3 - удалить файл
                4 - список файлов
                5 - Перейти в каталог

                -> 
                """);

            ConsoleKeyInfo key = Console.ReadKey();

            if (key.Key == ConsoleKey.NumPad4 || key.Key == ConsoleKey.D4)
            {
                string com = "list";
                byte[] buff = Encoding.UTF8.GetBytes(com);
                byte[] buffLength = { (byte)buff.Length };
                socket.Send(buffLength);
                socket.Send(buff);

                while(true)
                {
                    byte[] b = new byte[128];
                    socket.Receive(b);
                    string s = Encoding.UTF8.GetString(b);
                    Console.Write(s);
                    if (s.Contains(":END_OF_LIST"))
                        break;
                }
                Console.ReadKey();
            }
            
            else if (key.Key == ConsoleKey.NumPad2 || key.Key == ConsoleKey.D2)
            {
                string com = "upload";
                byte[] buff = Encoding.UTF8.GetBytes(com);
                byte[] buffLength = { (byte)buff.Length };
                socket.Send(buffLength);
                socket.Send(buff);

                Console.Write("\nКакой файл загрузить на сервер?: ");
                string sendFile = Console.ReadLine();
                io.uploadFile(socket, sendFile);
            }

            else if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
            {
                string com = "download";
                byte[] buff = Encoding.UTF8.GetBytes(com);
                byte[] buffLength = { (byte)buff.Length };
                socket.Send(buffLength);
                socket.Send(buff);

                Console.Write("\nКакой файл скачать?: ");
                string downloadFile = Console.ReadLine();
                byte[] b = Encoding.UTF8.GetBytes(downloadFile);
                byte[] getBlength = { (byte)b.Length };
                socket.Send(getBlength);
                socket.Send(b);
                
                Console.Write("В какую папку сохранить?: ");
                string getPath = Console.ReadLine();
                /*
                byte[] bb = Encoding.UTF8.GetBytes(downloadFile);
                byte[] getPathArrLength = { (byte)bb.Length };
                socket.Send(getPathArrLength);
                socket.Send(bb);
                */
                Console.WriteLine("[...] Идёт скачивание");
                io.downloadFile(socket, getPath);
                
            }

            else if (key.Key == ConsoleKey.NumPad3 || key.Key == ConsoleKey.D3)
            {
                string com = "delete";
                byte[] buff = Encoding.UTF8.GetBytes(com);
                byte[] buffLength = { (byte)buff.Length };
                socket.Send(buffLength);
                socket.Send(buff);

                Console.Write("Какой файл удалить?: ");
                string delFile = Console.ReadLine();
                byte[] b = Encoding.UTF8.GetBytes(delFile);
                byte[] getBlength = { (byte)b.Length };
                socket.Send(getBlength);
                socket.Send(b);
            }

            else if (key.Key == ConsoleKey.NumPad5 || key.Key == ConsoleKey.D5)
            {
                string com = "cd";
                byte[] buff = Encoding.UTF8.GetBytes(com);
                byte[] buffLength = { (byte)buff.Length };
                socket.Send(buffLength);
                socket.Send(buff);

                Console.Write("\nВ какой каталог перейти?: ");
                string dir = Console.ReadLine();
                byte[] b = Encoding.UTF8.GetBytes(dir);
                byte[] getBlength = { (byte)b.Length };
                socket.Send(getBlength);
                socket.Send(b);
            }
        }
    }
}
