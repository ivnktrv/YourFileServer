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
            Console.WriteLine($"[-] Папка не найдена: {dirName}");
            string[] dirNotFound = { $"\nПапки {dirName} не существует\n\n:END_OF_LIST" };
            return dirNotFound;
        }
    }

    public void uploadFile(Socket __socket, string file, string? folder = null)
    {
        try
        {
            BinaryReader b;

            if (folder == null)
                b = new(File.Open(file, FileMode.Open));
            else
                b = new(File.Open($"{folder}/{file}", FileMode.Open));


            byte[] getFileName = Encoding.UTF8.GetBytes(file);
            byte[] getFileName_arrLength = { (byte)getFileName.Length };
            byte[] getFileLength = BitConverter.GetBytes(b.BaseStream.Length);
            byte[] getFileLength_arrSize = { (byte)getFileLength.Length };
            b.Close();
            byte[] getFileChecksum;
            if (folder == null)
                getFileChecksum = Encoding.UTF8.GetBytes(sec.checksumFileSHA256(file));
            else
                getFileChecksum = Encoding.UTF8.GetBytes(sec.checksumFileSHA256($"{folder}/{file}"));

            __socket.Send(getFileName_arrLength);
            __socket.Send(getFileName);
            __socket.Send(getFileLength_arrSize);
            __socket.Send(getFileLength);
            __socket.Send(getFileChecksum);
            
            BinaryReader br;

            if (folder == null)
                br = new(File.Open(file, FileMode.Open));
            else
                br = new(File.Open($"{folder}/{file}", FileMode.Open));

            br.BaseStream.Position = 0;

            long c = 100000;
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                byte[] readByte = { br.ReadByte() };
                __socket.Send(readByte);
                if(br.BaseStream.Position == c)
                {
                    clearTerminal();
                    Console.WriteLine($"[UPLOAD] {file} [{br.BaseStream.Position / 1024} кб / {br.BaseStream.Length / 1024} кб]");
                    c += 100000;
                }
            }
            br.Close();
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[-] Файл не найден: {file}");
            byte[] getFileName_arrLengthZero = {0};
            __socket.Send(getFileName_arrLengthZero);
        }
    }

    public void downloadFile(Socket __socket, string saveFolder)
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

        long c = 100000;
        while (br.BaseStream.Position != fLength)
        {
            byte[] wr = new byte[1];
            __socket.Receive(wr);
            br.Write(wr[0]);
            if (br.BaseStream.Position == c)
            {
                clearTerminal();
                Console.WriteLine($"[DOWNLOAD] {Encoding.UTF8.GetString(getFileName)} [{br.BaseStream.Position / 1024} кб / {fLength / 1024} кб]");
                c += 100000;
            }
        }
        br.Close();

        string downloadedFileChecksum = sec.checksumFileSHA256(savePath);

        Console.WriteLine("[...] Проверка контрольной суммы");
        if (downloadedFileChecksum != Encoding.UTF8.GetString(getFileChecksum))
        {
            Console.WriteLine($"""
            [!] Хэши не совпадают

                {downloadedFileChecksum} (скачаный файл) != {Encoding.UTF8.GetString(getFileChecksum)} (файл на сервере)
            
            """);
            Console.ReadKey();
        }
    }

    public void clearTerminal()
    {
        Console.Clear();
        Console.WriteLine("\x1b[3J");
    }
}
