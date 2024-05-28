using System.Net.Sockets;
using System.Text;

namespace yfs_io;

public class YFSio
{
    public string[] getFiles(string dirName)
    {
        List<string> dirsAndFiles = [];

        string[] dirs = Directory.GetDirectories(dirName);
        foreach (string getDirs in dirs)
        {
            dirsAndFiles.Add($"\n{getDirs} ─┐");
        }

        string[] files = Directory.GetFiles(dirName);
        foreach (string getFiles in files)
        {
            dirsAndFiles.Add($"\n{getFiles}");
        }

        dirsAndFiles.Add("\n\n:END_OF_LIST"); // end of list

        return dirsAndFiles.ToArray();
    }

    public void uploadFile(Socket __socket, string file)
    {
        using BinaryReader br = new(File.Open(file, FileMode.Open));
        br.BaseStream.Position = 0;

        byte[] getFileName = Encoding.UTF8.GetBytes(file);
        byte[] getFileName_arrLength = { (byte)getFileName.Length };
        byte[] getFileLength = BitConverter.GetBytes(br.BaseStream.Length);
        byte[] getFileLength_arrSize = {(byte)getFileLength.Length};

        __socket.Send(getFileName_arrLength);
        __socket.Send(getFileName);
        __socket.Send(getFileLength_arrSize);
        __socket.Send(getFileLength);

        while (br.BaseStream.Position != br.BaseStream.Length)
        {
            byte[] readByte = {br.ReadByte()};
            __socket.Send(readByte);
        }
        //__socket.Send(Encoding.UTF8.GetBytes(":EOF")); // end of file
    }

    public void downloadFile(Socket __socket, string saveFolder)
    {
        byte[] getFileNameArrayLength = new byte[1];
        __socket.Receive(getFileNameArrayLength);

        byte[] getFileName = new byte[getFileNameArrayLength[0]];
        __socket.Receive(getFileName);

        byte[] getFileArrayLength = new byte[1];
        __socket.Receive(getFileArrayLength);
        
        byte[] getFileLength = new byte[getFileArrayLength[0]];
        __socket.Receive(getFileLength);

        using BinaryWriter br = new(File.Open($"{saveFolder}/{Encoding.UTF8.GetString(getFileName)}", FileMode.OpenOrCreate));
        long fLength = BitConverter.ToInt64(getFileLength);

        while (br.BaseStream.Position != fLength)
        {
            byte[] wr = new byte[1];
            __socket.Receive(wr);
            br.Write(wr[0]);
        }
    }
}
