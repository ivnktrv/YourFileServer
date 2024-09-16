using System.Text;

namespace yfs_keygen;

public class YFSkeygen
{
    private const string DICT = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()[]{}|/>_+-;:?<>.,";

    public string _keygen()
    {
        string key = "";
        for (int i = 0; i < new Random().Next(16, 64); i++)
        {
            key += DICT[new Random().Next(DICT.Length)];
        }
        return key;
    }

    public byte[] _keyBytes(string key)
    {
        return Encoding.ASCII.GetBytes(key);
    }
}
