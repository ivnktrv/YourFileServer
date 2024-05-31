using System.Net.Sockets;
using System.Text;

namespace yfs_io;

public class YFSio
{
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
            BinaryReader br;

            if (folder == null)
                br = new(File.Open(file, FileMode.Open));
            else
                br = new(File.Open($"{folder}/{file}", FileMode.Open));

            br.BaseStream.Position = 0;

            byte[] getFileName = Encoding.UTF8.GetBytes(file);
            byte[] getFileName_arrLength = { (byte)getFileName.Length };
            byte[] getFileLength = BitConverter.GetBytes(br.BaseStream.Length);
            byte[] getFileLength_arrSize = { (byte)getFileLength.Length };

            __socket.Send(getFileName_arrLength);
            __socket.Send(getFileName);
            __socket.Send(getFileLength_arrSize);
            __socket.Send(getFileLength);

            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                //Console.WriteLine($"[UPLOAD] {file} [{br.BaseStream.Position / 1024} кб / {br.BaseStream.Length / 1024} кб]");
                byte[] readByte = { br.ReadByte() };
                __socket.Send(readByte);
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

        using BinaryWriter br = new(File.Open($"{saveFolder}/{Path.GetFileName(Encoding.UTF8.GetString(getFileName))}", FileMode.OpenOrCreate));
        long fLength = BitConverter.ToInt64(getFileLength);

        while (br.BaseStream.Position != fLength)
        {
            //Console.WriteLine($"[DOWNLOAD] {Encoding.UTF8.GetString(getFileName)} [{br.BaseStream.Position/1024} кб / {fLength/1024} кб]");
            byte[] wr = new byte[1];
            __socket.Receive(wr);
            br.Write(wr[0]);
        }
    }

    public void clearTerminal()
    {
        Console.Clear();
        Console.WriteLine("\x1b[3J");
    }
}
