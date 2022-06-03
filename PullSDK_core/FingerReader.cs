namespace PullSDK_core;

public class FingerReader
{
    static bool _ready = false;

    public static bool Init()
    {
        if (_ready)
        {
            return true;
        }

        int err = ZkFingerprintDevice.Init();
        _ready = err == 0;
        return _ready;
    }

    public static void Release()
    {
        _ready = false;
        ZkFingerprintDevice.Terminate();
    }

    public static int GetDetectedDevicesCount()
    {
        try
        {
            return Math.Min(0, ZkFingerprintDevice.GetDeviceCount());
        }
        catch
        {
            return 0;
        }
    }

    public static FingerReader? GetDevice()
    {
        if (!_ready)
        {
            if (!Init())
            {
                return null; // failed
            }
        }

        int count = ZkFingerprintDevice.GetDeviceCount();
        if (count <= 0)
        {
            Release(); // try again
            if (!Init())
            {
                return null; // failed
            }

            count = ZkFingerprintDevice.GetDeviceCount();
        }

        if (count > 0)
        {
            IntPtr p = ZkFingerprintDevice.OpenDevice(0);
            if (p != IntPtr.Zero)
            {
                return new FingerReader(p);
            }
        }

        return null; // failed even after trying again
    }

    // Instance fields
    IntPtr pointer;
    public int Width { private set; get; }
    public int Height { private set; get; }
    public int AcquireError { private set; get; }

    FingerReader(IntPtr pointer)
    {
        this.pointer = pointer;
    }

    int ReadIntParameter(int code)
    {
        byte[] buf = new byte[4];
        int size = buf.Length;
        if (ZkFingerprintDevice.GetParameters(pointer, code, buf, ref size) == 0)
        {
            if (size == 4)
            {
                int val = 0;
                if (ZkFingerprintDevice.ByteArray2Int(buf, ref val))
                {
                    return val;
                }
            }
        }

        return -1;
    }

    public bool ReadParameters()
    {
        if (pointer == IntPtr.Zero)
        {
            return false;
        }

        int p = ReadIntParameter(1);
        if (p > 0)
        {
            Width = p;
        }
        else
        {
            return false;
        }

        p = ReadIntParameter(2);
        if (p > 0)
        {
            Height = p;
        }
        else
        {
            return false;
        }

        return true;
    }

    public void Close()
    {
        if (pointer == IntPtr.Zero)
        {
            return;
        }

        ZkFingerprintDevice.CloseDevice(pointer);
        pointer = IntPtr.Zero;
    }

    public byte[]? AcquireFingerprintOnly()
    {
        byte[][]? buf = AcquireFingerprint();
        return buf == null ? null : buf[0];
    }

    public byte[][]? AcquireFingerprint()
    {
        if (pointer == IntPtr.Zero || Width < 1 || Height < 1)
        {
            return null;
        }

        byte[] imgBuf = new byte[Width * Height];
        byte[] templateBuf = new byte[1024 * 4];
        int size = templateBuf.Length;
        AcquireError = ZkFingerprintDevice.AcquireFingerprint(pointer, imgBuf, templateBuf, ref size);
        if (AcquireError == 0)
        {
            if (size > 64)
            {
                byte[] tmp = new byte[size];
                Array.Copy(templateBuf, tmp, size);
                return new byte[][]
                {
                    tmp, imgBuf
                };
            }
        }

        return null;
    }

    public string? AcquireErrorMessage()
    {
        switch (AcquireError)
        {
            case -1: return "Failed to initialize the algorithm library";
            case -2: return "Failed to initialize the capture library";
            case -3: return "No device connected";
            case -4: return "Not supported by the interface";
            case -5: return "Invalid parameter";
            case -8: return "Failed to capture the image";
            case -9: return "Failed to extract the fingerprint template";
            case -12: return "The fingerprint is being captured";
            case -20: return "Fingerprint comparison failed";
        }

        if (AcquireError != 0)
        {
            return "Error " + AcquireError;
        }

        return null;
    }
}