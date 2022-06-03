namespace PullSDK_core;

public class FingerprintDb
{
    IntPtr _pointer = IntPtr.Zero;
    byte[]?[] _templates = new byte[3][];

    public int Size { private set; get; }

    public static int Steps => 3;

    public FingerprintDb()
    {
        Size = 0;
    }

    public bool Init()
    {
        if (_pointer == IntPtr.Zero)
        {
            _pointer = ZkFingerprintDevice.DBInit();
            if (_pointer != IntPtr.Zero)
            {
                for (int i = 0; i < Steps; i++)
                {
                    _templates[i] = null;
                }
                return true;
            }
        }

        return false;
    }

    public bool Free()
    {
        if (_pointer == IntPtr.Zero) return true;
        if (0 == ZkFingerprintDevice.DBFree(_pointer))
        {
            _pointer = IntPtr.Zero;
            return true;
        }

        return false;
    }

    public bool Add(byte[] template)
    {
        if (_pointer == IntPtr.Zero || Size >= 3)
        {
            return false;
        }

        if (0 == ZkFingerprintDevice.DBAdd(_pointer, Size + 1, template))
        {
            _templates[Size] = template;
            Size++;
            return true;
        }

        return false;
    }

    public byte[]? GenerateTemplate()
    {
        if (Size == Steps)
        {
            byte[] template = new byte[4096];
            int length = template.Length;
            if (ZkFingerprintDevice.DBMerge(_pointer, _templates[0] ?? Array.Empty<byte>(), _templates[1] ?? Array.Empty<byte>(), _templates[2] ?? Array.Empty<byte>(), template, ref length) == 0)
            {
                byte[] result = new byte[length];
                Array.Copy(template, result, length);
                return result;
            }
        }

        return null;
    }
}