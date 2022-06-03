using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PullSDK_core;

public static class ZkFingerprintDevice
{
    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_Init();

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_Terminate();

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_GetDeviceCount();

    [DllImport("libzkfp.dll")]
    private static extern IntPtr ZKFPM_OpenDevice(int index);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_CloseDevice(IntPtr handle);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_GetCaptureParamsEx(IntPtr handle, ref int width, ref int height, ref int dpi);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_SetParameters(IntPtr handle, int nParamCode, IntPtr paramValue, int cbParamValue);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_GetParameters(IntPtr handle, int nParamCode, IntPtr paramValue, ref int cbParamValue);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_AcquireFingerprint(IntPtr handle, IntPtr fpImage, uint cbFPImage, IntPtr fpTemplate, ref int cbTemplate);

    [DllImport("libzkfp.dll")]
    private static extern IntPtr ZKFPM_CreateDBCache();

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_CloseDBCache(IntPtr hDBCache);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_GenRegTemplate(IntPtr hDBCache, IntPtr temp1, IntPtr temp2, IntPtr temp3, IntPtr regTemp, ref int cbRegTemp);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_AddRegTemplateToDBCache(IntPtr hDBCache, uint fid, IntPtr fpTemplate, uint cbTemplate);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_DelRegTemplateFromDBCache(IntPtr hDBCache, uint fid);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_ClearDBCache(IntPtr hDBCache);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_GetDBCacheCount(IntPtr hDBCache, ref int count);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_Identify(IntPtr hDBCache, IntPtr fpTemplate, uint cbTemplate, ref int FID, ref int score);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_VerifyByID(IntPtr hDBCache, uint fid, IntPtr fpTemplate, uint cbTemplate);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_MatchFinger(IntPtr hDBCache, IntPtr fpTemplate1, uint cbTemplate1, IntPtr fpTemplate2, uint cbTemplate2);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_ExtractFromImage(IntPtr hDBCache, string FilePathName, int DPI, IntPtr fpTemplate, ref int cbTemplate);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_AcquireFingerprintImage(IntPtr hDBCache, IntPtr fpImage, uint cbFPImage);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_Base64ToBlob(string src, IntPtr blob, uint cbBlob);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_BlobToBase64(IntPtr src, uint cbSrc, StringBuilder dst, uint cbDst);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_DBSetParameter(IntPtr handle, int nParamCode, int paramValue);

    [DllImport("libzkfp.dll")]
    private static extern int ZKFPM_DBGetParameter(IntPtr handle, int nParamCode, ref int paramValue);

    public static int Init() => ZkFingerprintDevice.ZKFPM_Init();

    public static int Terminate() => ZkFingerprintDevice.ZKFPM_Terminate();

    public static int GetDeviceCount() => ZkFingerprintDevice.ZKFPM_GetDeviceCount();

    public static IntPtr OpenDevice(int index) => ZkFingerprintDevice.ZKFPM_OpenDevice(index);

    public static int CloseDevice(IntPtr devHandle) => ZkFingerprintDevice.ZKFPM_CloseDevice(devHandle);

    public static int SetParameters(IntPtr devHandle, int code, byte[] pramValue, int size)
    {
        if (IntPtr.Zero == devHandle)
            return ZkFingerprintDeviceDef.ZkfpErrInvalidHandle;
        return pramValue == null || pramValue.Length < size || size <= 0 ? ZkFingerprintDeviceDef.ZkfpErrInvalidParam : ZkFingerprintDevice.ZKFPM_SetParameters(devHandle, code, Marshal.UnsafeAddrOfPinnedArrayElement((Array) pramValue, 0), size);
    }

    public static int GetParameters(IntPtr devHandle, int code, byte[] paramValue, ref int size)
    {
        if (IntPtr.Zero == devHandle)
            return ZkFingerprintDeviceDef.ZkfpErrInvalidHandle;
        size = paramValue.Length;
        return ZkFingerprintDevice.ZKFPM_GetParameters(devHandle, code, Marshal.UnsafeAddrOfPinnedArrayElement((Array) paramValue, 0), ref size);
    }

    public static int AcquireFingerprint(IntPtr devHandle, byte[] imgBuffer, byte[] template, ref int size)
    {
        if (IntPtr.Zero == devHandle)
            return ZkFingerprintDeviceDef.ZkfpErrInvalidHandle;
        size = template.Length;
        return ZkFingerprintDevice.ZKFPM_AcquireFingerprint(devHandle, Marshal.UnsafeAddrOfPinnedArrayElement((Array) imgBuffer, 0), (uint) imgBuffer.Length, Marshal.UnsafeAddrOfPinnedArrayElement((Array) template, 0), ref size);
    }

    public static int AcquireFingerprintImage(IntPtr devHandle, byte[] imgbuf) => IntPtr.Zero == devHandle ? ZkFingerprintDeviceDef.ZkfpErrInvalidHandle : ZkFingerprintDevice.ZKFPM_AcquireFingerprintImage(devHandle, Marshal.UnsafeAddrOfPinnedArrayElement((Array) imgbuf, 0), (uint) imgbuf.Length);

    public static IntPtr DBInit() => ZkFingerprintDevice.ZKFPM_CreateDBCache();

    public static int DBFree(IntPtr dbHandle) => ZkFingerprintDevice.ZKFPM_ClearDBCache(dbHandle);

    public static int DBSetParameter(IntPtr dbHandle, int code, int value) => ZkFingerprintDevice.ZKFPM_DBSetParameter(dbHandle, code, value);

    public static int DBGetParameter(IntPtr dbHandle, int code, ref int value) => ZkFingerprintDevice.ZKFPM_DBGetParameter(dbHandle, code, ref value);

    public static int DBMerge(IntPtr dbHandle, byte[] temp1, byte[] temp2, byte[] temp3, byte[] regTemp, ref int regTempLen)
    {
        if (IntPtr.Zero == dbHandle)
            return ZkFingerprintDeviceDef.ZkfpErrInvalidHandle;
        regTempLen = regTemp.Length;
        return ZkFingerprintDevice.ZKFPM_GenRegTemplate(dbHandle, Marshal.UnsafeAddrOfPinnedArrayElement((Array) temp1, 0), Marshal.UnsafeAddrOfPinnedArrayElement((Array) temp2, 0), Marshal.UnsafeAddrOfPinnedArrayElement((Array) temp3, 0), Marshal.UnsafeAddrOfPinnedArrayElement((Array) regTemp, 0), ref regTempLen);
    }

    public static int DBAdd(IntPtr dbHandle, int fid, byte[] regTemp) => IntPtr.Zero == dbHandle ? ZkFingerprintDeviceDef.ZkfpErrInvalidHandle : ZkFingerprintDevice.ZKFPM_AddRegTemplateToDBCache(dbHandle, (uint) fid, Marshal.UnsafeAddrOfPinnedArrayElement((Array) regTemp, 0), (uint) regTemp.Length);

    public static int DBDel(IntPtr dbHandle, int fid) => IntPtr.Zero == dbHandle ? ZkFingerprintDeviceDef.ZkfpErrInvalidHandle : ZkFingerprintDevice.ZKFPM_DelRegTemplateFromDBCache(dbHandle, (uint) fid);

    public static int DBClear(IntPtr dbHandle) => IntPtr.Zero == dbHandle ? ZkFingerprintDeviceDef.ZkfpErrInvalidHandle : ZkFingerprintDevice.ZKFPM_ClearDBCache(dbHandle);

    public static int DBCount(IntPtr dbHandle)
    {
        if (IntPtr.Zero == dbHandle)
            return ZkFingerprintDeviceDef.ZkfpErrInvalidHandle;
        int count = 0;
        ZkFingerprintDevice.ZKFPM_GetDBCacheCount(dbHandle, ref count);
        return count;
    }

    public static int DBIdentify(IntPtr dbHandle, byte[] temp, ref int fid, ref int score) => IntPtr.Zero == dbHandle ? ZkFingerprintDeviceDef.ZkfpErrInvalidHandle : ZkFingerprintDevice.ZKFPM_Identify(dbHandle, Marshal.UnsafeAddrOfPinnedArrayElement((Array) temp, 0), (uint) temp.Length, ref fid, ref score);

    public static int DBMatch(IntPtr dbHandle, byte[] temp1, byte[] temp2) => IntPtr.Zero == dbHandle ? ZkFingerprintDeviceDef.ZkfpErrInvalidHandle : ZkFingerprintDevice.ZKFPM_MatchFinger(dbHandle, Marshal.UnsafeAddrOfPinnedArrayElement((Array) temp1, 0), (uint) temp1.Length, Marshal.UnsafeAddrOfPinnedArrayElement((Array) temp2, 0), (uint) temp2.Length);

    public static int ExtractFromImage(IntPtr dbHandle, string FileName, int DPI, byte[] template, ref int size)
    {
        return IntPtr.Zero == dbHandle ? ZkFingerprintDeviceDef.ZkfpErrNotInit : ZkFingerprintDevice.ZKFPM_ExtractFromImage(dbHandle, FileName, DPI, Marshal.UnsafeAddrOfPinnedArrayElement((Array) template, 0), ref size);
    }

    public static byte[]? Base64ToBlob(string? base64Str)
    {
        if (base64Str == null || base64Str.Length <= 0 || base64Str.Length % 4 != 0)
            return null;
        int cbBlob = base64Str.Length / 4;
        byte[] numArray = new byte[cbBlob];
        int blob = ZkFingerprintDevice.ZKFPM_Base64ToBlob(base64Str, Marshal.UnsafeAddrOfPinnedArrayElement((Array) numArray, 0), (uint) cbBlob);
        if (blob <= 0)
            return null;
        byte[] destinationArray = new byte[cbBlob];
        Array.Copy((Array) numArray, (Array) destinationArray, blob);
        return destinationArray;
    }

    public static string BlobToBase64(byte[] blob, int nDataLen)
    {
        if (blob == null || blob.Length <= 0 || nDataLen <= 0 || blob.Length < nDataLen)
            return "";
        int num = blob.Length / 3 * 4 + 1;
        StringBuilder dst = new StringBuilder(num);
        return ZkFingerprintDevice.ZKFPM_BlobToBase64(Marshal.UnsafeAddrOfPinnedArrayElement((Array) blob, 0), (uint) nDataLen, dst, (uint) num) > 0 ? dst.ToString() : "";
    }

    public static bool ByteArray2Int(byte[] buf, ref int value)
    {
        if (buf.Length < 4)
            return false;
        value = BitConverter.ToInt32(buf, 0);
        return true;
    }

    public static bool Int2ByteArray(int value, byte[] buf)
    {
        if (buf == null || buf.Length < 4)
            return false;
        buf[0] = (byte) (value & (int) byte.MaxValue);
        buf[1] = (byte) ((value & 65280) >> 8);
        buf[2] = (byte) ((value & 16711680) >> 16);
        buf[3] = (byte) (value >> 24 & (int) byte.MaxValue);
        return true;
    }
}