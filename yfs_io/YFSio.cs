using System.Net.Sockets;
using System.Text;
using yfs_security;

namespace yfs_io;

public class YFSio
{
    YFSsec sec = new();

    public string[] getFiles(string dirName)
    {
        List<string> dirsAndFiles = [];

        try
        {
            string[] dirs = Directory.GetDirectories(dirName);
            foreach (string getDirs in dirs)
            {
                dirsAndFiles.Add($"\n{getDirs}");
            }

            string[] files = Directory.GetFiles(dirName);
            foreach (string getFiles in files)
            {
                dirsAndFiles.Add($"\n{Path.GetFileName(getFiles)}");
            }

            dirsAndFiles.Add("\n\n:END_OF_LIST"); // end of list

            return dirsAndFiles.ToArray();
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Папка не найдена: {dirName}");
            string[] dirNotFound = { $"\nПапки {dirName} не существует\n\n:END_OF_LIST" };
            return dirNotFound;
        }
    }

    public void uploadFile(Socket __socket, string file, 
        string? folder = null, bool isServer = false)
    {
        try
        {
            string path;
            if (folder == null)
                path = file;
            else
                path = $"{folder}/{file}";

            BinaryReader b = new(File.Open(path, FileMode.Open));

            if (isServer)
                Console.WriteLine($"[{DateTime.Now}] Подготовка к отправке файла {file}");
            
            byte[] getFileName = Encoding.UTF8.GetBytes(file);
            byte[] getFileName_arrLength = { (byte)getFileName.Length };
            byte[] getFileLength = BitConverter.GetBytes(b.BaseStream.Length);
            byte[] getFileLength_arrSize = { (byte)getFileLength.Length };
            b.Close();
            byte[] getFileChecksum = Encoding.UTF8.GetBytes(sec.checksumFileSHA256(path));

            __socket.Send(getFileName_arrLength);
            __socket.Send(getFileName);
            __socket.Send(getFileLength_arrSize);
            __socket.Send(getFileLength);
            __socket.Send(getFileChecksum);
            
            BinaryReader br = new(File.Open(path, FileMode.Open));
            br.BaseStream.Position = 0;
            
            if (isServer)
                Console.WriteLine($"[{DateTime.Now}] Файл отправляется: {file}");

            long c = 100000;
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                byte[] readByte = { br.ReadByte() };
                __socket.Send(readByte);
                if (br.BaseStream.Position == c && !isServer)
                {
                    clearTerminal();
                    Console.WriteLine($"[UPLOAD] {file} [{br.BaseStream.Position / 1024} кб / {br.BaseStream.Length / 1024} кб]");
                    c += 100000;
                }
            }
            br.Close();

            if (isServer)
                Console.WriteLine($"[{DateTime.Now}] Проверка контрольной суммы");
            
            byte[] uploadedFileChecksum = new byte[64];
            __socket.Receive(uploadedFileChecksum);

            if (Encoding.UTF8.GetString(getFileChecksum) != Encoding.UTF8.GetString(uploadedFileChecksum))
            {
                if (!isServer)
                {
                    Console.WriteLine($"""
                    [!] Хеши не совпадают. Возможно, файл при отправки на сервер был повреждён.

                    SHA-256:
                        {Encoding.UTF8.GetString(getFileChecksum)} (отправленный файл) 
                                ≠
                        {Encoding.UTF8.GetString(uploadedFileChecksum)} (файл на сервере)
            
                    [1] Удалить файл  [2] Оставить
                    
                    => 
                    """);
                    ConsoleKeyInfo key = Console.ReadKey();

                    if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
                    {
                        byte[] sendAnswer = { 1 };
                        __socket.Send(sendAnswer);
                    }

                    else
                    {
                        byte[] sendAnswer = { 0 };
                        __socket.Send(sendAnswer);
                    }
                }
                else
                {
                    Console.WriteLine($"""
                    [{DateTime.Now}] [-] Хеши не совпадают

                    SHA-256:
                        {Encoding.UTF8.GetString(getFileChecksum)} (отправленный файл)
                                   ≠
                        {Encoding.UTF8.GetString(uploadedFileChecksum)} (файл на клиенте)
                    
                    """);
                } 
            }

            else
            {
                if (isServer)
                {
                    Console.WriteLine($"""
                    [{DateTime.Now}] [+] Хеши совпадают

                    SHA-256:
                        {Encoding.UTF8.GetString(getFileChecksum)} (отправленный файл)
                                    =
                        {Encoding.UTF8.GetString(uploadedFileChecksum)} (файл на клиенте)
                   
                    """);
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[{DateTime.Now}] [-] Файл не найден: {file}");
            byte[] getFileName_arrLengthZero = {0};
            __socket.Send(getFileName_arrLengthZero);
        }
    }

    public void downloadFile(Socket __socket, string saveFolder, 
        bool isServer = false)
    {
        byte[] getFileNameArrayLength = new byte[1];
        __socket.Receive(getFileNameArrayLength);

        if (getFileNameArrayLength[0] == 0)
        {
            Console.WriteLine("[-] Файл не найден");
            return;
        }

        byte[] getFileName = new byte[getFileNameArrayLength[0]];
        __socket.Receive(getFileName);

        byte[] getFileArrayLength = new byte[1];
        __socket.Receive(getFileArrayLength);
        
        byte[] getFileLength = new byte[getFileArrayLength[0]];
        __socket.Receive(getFileLength);

        byte[] getFileChecksum = new byte[64];
        __socket.Receive(getFileChecksum);

        string savePath = $"{saveFolder}/{Path.GetFileName(Encoding.UTF8.GetString(getFileName))}";
        using BinaryWriter br = new(File.Open(savePath, FileMode.OpenOrCreate));
        long fLength = BitConverter.ToInt64(getFileLength);

        if (isServer)
            Console.WriteLine($"[{DateTime.Now}] [i] Принимаю файл: {Path.GetFileName(Encoding.UTF8.GetString(getFileName))}");
        
        long c = 100000;
        while (br.BaseStream.Position != fLength)
        {
            byte[] wr = new byte[1];
            __socket.Receive(wr);
            br.Write(wr[0]);
            if (br.BaseStream.Position == c && !isServer)
            {
                clearTerminal();
                Console.WriteLine($"[DOWNLOAD] {Encoding.UTF8.GetString(getFileName)} [{br.BaseStream.Position / 1024} кб / {fLength / 1024} кб]");
                c += 100000;
            }
        }
        br.Close();

        string downloadedFileChecksum = sec.checksumFileSHA256(savePath);
        byte[] downloadedFileChecksum_bytes = Encoding.UTF8.GetBytes(downloadedFileChecksum);
        __socket.Send(downloadedFileChecksum_bytes);

        Console.WriteLine("[...] Проверка контрольной суммы");
        if (downloadedFileChecksum != Encoding.UTF8.GetString(getFileChecksum))
        {
            if (!isServer)
            {
                Console.WriteLine($"""
                [!] Хеши не совпадают. Возможно, файл при скачивании был повреждён.

                SHA-256:
                    {downloadedFileChecksum} (скачаный файл) 
                            ≠
                    {Encoding.UTF8.GetString(getFileChecksum)} (файл на сервере)
            
                [1] Удалить файл  [2] Оставить
            
                => 
                """);
                ConsoleKeyInfo key = Console.ReadKey();

                if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
                {
                    File.Delete(savePath);
                }

                else if (key.Key == ConsoleKey.NumPad2 || key.Key == ConsoleKey.D2) { }
            }
            else
            {
                Console.WriteLine($"""
                    [{DateTime.Now}] [-] Хеши не совпадают

                    SHA-256:
                        {downloadedFileChecksum} (скачаный файл)
                                   ≠
                        {Encoding.UTF8.GetString(getFileChecksum)} (файл на клиенте)
                    
                    """);

                byte[] wrongHash = Encoding.UTF8.GetBytes(downloadedFileChecksum);
                __socket.Send(wrongHash);

                byte[] getAnswer = new byte[1];
                __socket.Receive(getAnswer);

                if (getAnswer[0] == 0)
                    File.Delete(savePath);
                else { }
            }
        }
        else
        {
            if (isServer)
            {
                Console.WriteLine($"""
                [{DateTime.Now}] [+] Хеши совпадают

                SHA-256:
                    {downloadedFileChecksum} (скачаный файл)
                                =
                    {Encoding.UTF8.GetString(getFileChecksum)} (файл на клиенте)
                
                """);
            }
        }
    }

    public void clearTerminal()
    {
        Console.Clear();
        Console.WriteLine("\x1b[3J");
    }
}
