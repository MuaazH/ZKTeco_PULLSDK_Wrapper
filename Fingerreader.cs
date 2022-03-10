using System;
using System.IO;
using System.Runtime.InteropServices;
using libzkfpcsharp;

namespace i04PullSDK
{

	public class Fingerreader
	{
		static bool ready = false;

		public static bool Init() {
			if (ready) {
				return true;
			}
			int err = zkfp2.Init();
			ready = err == 0;
			return ready;
		}
		
		public static void Release() {
			ready = false;
			zkfp2.Terminate();
		}
		
		public static int GetDetectedDevicesCount() {
			try {
				return Math.Min(0, zkfp2.GetDeviceCount());
			} catch {
				return 0;
			}
		}
		
		public static Fingerreader GetDevice() {
			if (!ready) {
				if (!Init()) {
					return null; // failed
				}
			}
			int count = zkfp2.GetDeviceCount();
			if (count <= 0) {
				Release(); // try again
				if (!Init()) {
					return null; // failed
				}
				count = zkfp2.GetDeviceCount();
			}
			if (count > 0) {
				IntPtr p = zkfp2.OpenDevice(0);
				if (p != IntPtr.Zero) {
					return new Fingerreader(p);
				}
			}
			return null; // failed even after trying again
		}

		// Instance fields
		IntPtr pointer;
		public int Width { private set; get; }
		public int Height { private set; get; }
		public int AcquireError { private set; get; }
		
		Fingerreader(IntPtr pointer)
		{
			this.pointer = pointer;
		}
		
		int ReadIntParameter(int code) {
			byte[] buf = new byte[4];
			int size = buf.Length;
			if (zkfp2.GetParameters(pointer, code, buf, ref size) == 0) {
				if (size == 4) {
					int val = 0;
					if (zkfp2.ByteArray2Int(buf, ref val)) {
						return val;
					}
				}
			}
			return -1;
		}

		public bool ReadParameters() {
			if (pointer == IntPtr.Zero) {
				return false;
			}
			int p = ReadIntParameter(1);
			if (p > 0) {
				Width = p;
			} else {
				return false;
			}
			p = ReadIntParameter(2);
			if (p > 0) {
				Height = p;
			} else {
				return false;
			}
			return true;
		}

		public void Close() {
			if (pointer == IntPtr.Zero) {
				return;
			}
			zkfp2.CloseDevice(pointer);
			pointer = IntPtr.Zero;
		}

		public byte[] AcquireFingerprintOnly() {
			byte[][] buf = AcquireFingerprint();
			return buf == null ? null : buf[0];
		}
		
		public byte[][] AcquireFingerprint() {
			if (pointer == IntPtr.Zero || Width < 1 || Height < 1) {
				return null;
			}
			byte[] imgBuf = new byte[Width * Height];
			byte[] templateBuf = new byte[1024*4];
			int size = templateBuf.Length;
			AcquireError = zkfp2.AcquireFingerprint(pointer, imgBuf, templateBuf, ref size);
			if (AcquireError == 0) {
				if (size > 64) {
					byte[] tmp = new byte[size];
					Array.Copy(templateBuf, tmp, size);
					return new byte[][] {
						tmp, imgBuf
					};
				}
			}
			return null;
		}

		public string AcquireErrorMessage() {
			switch (AcquireError) {
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
			if (AcquireError != 0) {
				return "Error " + AcquireError;
			}
			return null;
		}
		
	}

	public class FingerprintDB {
		
		IntPtr pointer = IntPtr.Zero;
		byte[][] templates;
		public int Size { private set; get; }

		public static int Steps {
			get {
				return 3;
			}
		}
		
		public FingerprintDB() {
			Size = 0;
		}
		
		public bool Init() {
			if (pointer == IntPtr.Zero) {
				pointer = zkfp2.DBInit();
				if (pointer != IntPtr.Zero) {
					templates = new byte[3][];
					return true;
				}
			}
			return false;
		}

		public bool Free() {
			if (pointer == IntPtr.Zero) return true;
			if (0 == zkfp2.DBFree(pointer)) {
				pointer = IntPtr.Zero;
				return true;
			}
			return false;
		}
		
		public bool Add(byte[] template) {
			if (pointer == IntPtr.Zero || Size >= 3) {
				return false;
			}
			if (0 == zkfp2.DBAdd(pointer, Size + 1, template)) {
				templates[Size] = template;
				Size++;
				return true;
			}
			return false;
		}

		public byte[] GenerateTemplate() {
			if (Size == 3) {
				byte[] template = new byte[4096];
				int length = template.Length;
				if (zkfp2.DBMerge(pointer, templates[0], templates[1], templates[2], template, ref length) == 0) {
					byte[] result = new byte[length];
					Array.Copy(template, result, length);
					return result;
				}
			}
			return null;
		}

	}
	
	public class BitmapFormat
    {
        public struct BITMAPFILEHEADER
        {
            public ushort bfType;
            public int bfSize;
            public ushort bfReserved1;
            public ushort bfReserved2;
            public int bfOffBits;
        }

        public struct MASK
        {
            public byte redmask;
            public byte greenmask;
            public byte bluemask;
            public byte rgbReserved;
        }

        public struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        /*******************************************
        * 函数名称：RotatePic       
        * 函数功能：旋转图片，目的是保存和显示的图片与按的指纹方向不同     
        * 函数入参：BmpBuf---旋转前的指纹字符串
        * 函数出参：ResBuf---旋转后的指纹字符串
        * 函数返回：无
        *********************************************/
        public static void RotatePic(byte[] BmpBuf, int width, int height, ref byte[] ResBuf)
        {
            int RowLoop = 0;
            int ColLoop = 0;
            int BmpBuflen = width * height;

            try
            {
                for (RowLoop = 0; RowLoop < BmpBuflen; )
                {
                    for (ColLoop = 0; ColLoop < width; ColLoop++)
                    {
                        ResBuf[RowLoop + ColLoop] = BmpBuf[BmpBuflen - RowLoop - width + ColLoop];
                    }

                    RowLoop = RowLoop + width;
                }
            }
            catch
            {
                //ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
                //logger.Append();
            }
        }

        /*******************************************
        * 函数名称：StructToBytes       
        * 函数功能：将结构体转化成无符号字符串数组     
        * 函数入参：StructObj---被转化的结构体
        *           Size---被转化的结构体的大小
        * 函数出参：无
        * 函数返回：结构体转化后的数组
        *********************************************/
        public static byte[] StructToBytes(object StructObj, int Size)
        {
            int StructSize = Marshal.SizeOf(StructObj);
            byte[] GetBytes = new byte[StructSize];

            try
            {
                IntPtr StructPtr = Marshal.AllocHGlobal(StructSize);
                Marshal.StructureToPtr(StructObj, StructPtr, false);
                Marshal.Copy(StructPtr, GetBytes, 0, StructSize);
                Marshal.FreeHGlobal(StructPtr);

                if (Size == 14)
                {
                    byte[] NewBytes = new byte[Size];
                    int Count = 0;
                    int Loop = 0;

                    for (Loop = 0; Loop < StructSize; Loop++)
                    {
                        if (Loop != 2 && Loop != 3)
                        {
                            NewBytes[Count] = GetBytes[Loop];
                            Count++;
                        }
                    }

                    return NewBytes;
                }
                else
                {
                    return GetBytes;
                }
            }
            catch
            {
                //ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
                //logger.Append();

                return GetBytes;
            }
        }

        /*******************************************
        * 函数名称：GetBitmap       
        * 函数功能：将传进来的数据保存为图片     
        * 函数入参：buffer---图片数据
        *           nWidth---图片的宽度
        *           nHeight---图片的高度
        * 函数出参：无
        * 函数返回：无
        *********************************************/
        public static void GetBitmap(byte[] buffer, int nWidth, int nHeight, ref MemoryStream ms)
        {
            int ColorIndex = 0;
            ushort m_nBitCount = 8;
            int m_nColorTableEntries = 256;
            byte[] ResBuf = new byte[nWidth * nHeight*2];

            try
            {
                BITMAPFILEHEADER BmpHeader = new BITMAPFILEHEADER();
                BITMAPINFOHEADER BmpInfoHeader = new BITMAPINFOHEADER();
                MASK[] ColorMask = new MASK[m_nColorTableEntries];

                int w = (((nWidth + 3) / 4) * 4);

                //图片头信息
                BmpInfoHeader.biSize = Marshal.SizeOf(BmpInfoHeader);
                BmpInfoHeader.biWidth = nWidth;
                BmpInfoHeader.biHeight = nHeight;
                BmpInfoHeader.biPlanes = 1;
                BmpInfoHeader.biBitCount = m_nBitCount;
                BmpInfoHeader.biCompression = 0;
                BmpInfoHeader.biSizeImage = 0;
                BmpInfoHeader.biXPelsPerMeter = 0;
                BmpInfoHeader.biYPelsPerMeter = 0;
                BmpInfoHeader.biClrUsed = m_nColorTableEntries;
                BmpInfoHeader.biClrImportant = m_nColorTableEntries;

                //文件头信息
                BmpHeader.bfType = 0x4D42;
                BmpHeader.bfOffBits = 14 + Marshal.SizeOf(BmpInfoHeader) + BmpInfoHeader.biClrUsed * 4;
                BmpHeader.bfSize = BmpHeader.bfOffBits + ((((w * BmpInfoHeader.biBitCount + 31) / 32) * 4) * BmpInfoHeader.biHeight);
                BmpHeader.bfReserved1 = 0;
                BmpHeader.bfReserved2 = 0;

                ms.Write(StructToBytes(BmpHeader, 14), 0, 14);
                ms.Write(StructToBytes(BmpInfoHeader, Marshal.SizeOf(BmpInfoHeader)), 0, Marshal.SizeOf(BmpInfoHeader));

                //调试板信息
                for (ColorIndex = 0; ColorIndex < m_nColorTableEntries; ColorIndex++)
                {
                    ColorMask[ColorIndex].redmask = (byte)ColorIndex;
                    ColorMask[ColorIndex].greenmask = (byte)ColorIndex;
                    ColorMask[ColorIndex].bluemask = (byte)ColorIndex;
                    ColorMask[ColorIndex].rgbReserved = 0;

                    ms.Write(StructToBytes(ColorMask[ColorIndex], Marshal.SizeOf(ColorMask[ColorIndex])), 0, Marshal.SizeOf(ColorMask[ColorIndex]));
                }

                //图片旋转，解决指纹图片倒立的问题
                RotatePic(buffer, nWidth, nHeight, ref ResBuf);

                byte[] filter = null;
                if (w - nWidth > 0)
                {
                    filter = new byte[w - nWidth];
                }
                for (int i = 0; i < nHeight; i++)
                {
                    ms.Write(ResBuf, i * nWidth, nWidth);
                    if (w - nWidth > 0)
                    {
                        ms.Write(ResBuf, 0, w - nWidth);
                    }
                }
            }
            catch
            {
               // ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
               // logger.Append();
            }
        }

        /*******************************************
        * 函数名称：WriteBitmap       
        * 函数功能：将传进来的数据保存为图片     
        * 函数入参：buffer---图片数据
        *           nWidth---图片的宽度
        *           nHeight---图片的高度
        * 函数出参：无
        * 函数返回：无
        *********************************************/
        public static void WriteBitmap(byte[] buffer, int nWidth, int nHeight)
        {
            int ColorIndex = 0;
            ushort m_nBitCount = 8;
            int m_nColorTableEntries = 256;
            byte[] ResBuf = new byte[nWidth * nHeight];

            try
            {

                BITMAPFILEHEADER BmpHeader = new BITMAPFILEHEADER();
                BITMAPINFOHEADER BmpInfoHeader = new BITMAPINFOHEADER();
                MASK[] ColorMask = new MASK[m_nColorTableEntries];
                int w = (((nWidth + 3) / 4) * 4);
                //图片头信息
                BmpInfoHeader.biSize = Marshal.SizeOf(BmpInfoHeader);
                BmpInfoHeader.biWidth = nWidth;
                BmpInfoHeader.biHeight = nHeight;
                BmpInfoHeader.biPlanes = 1;
                BmpInfoHeader.biBitCount = m_nBitCount;
                BmpInfoHeader.biCompression = 0;
                BmpInfoHeader.biSizeImage = 0;
                BmpInfoHeader.biXPelsPerMeter = 0;
                BmpInfoHeader.biYPelsPerMeter = 0;
                BmpInfoHeader.biClrUsed = m_nColorTableEntries;
                BmpInfoHeader.biClrImportant = m_nColorTableEntries;

                //文件头信息
                BmpHeader.bfType = 0x4D42;
                BmpHeader.bfOffBits = 14 + Marshal.SizeOf(BmpInfoHeader) + BmpInfoHeader.biClrUsed * 4;
                BmpHeader.bfSize = BmpHeader.bfOffBits + ((((w * BmpInfoHeader.biBitCount + 31) / 32) * 4) * BmpInfoHeader.biHeight);
                BmpHeader.bfReserved1 = 0;
                BmpHeader.bfReserved2 = 0;

                Stream FileStream = File.Open("finger.bmp", FileMode.Create, FileAccess.Write);
                BinaryWriter TmpBinaryWriter = new BinaryWriter(FileStream);

                TmpBinaryWriter.Write(StructToBytes(BmpHeader, 14));
                TmpBinaryWriter.Write(StructToBytes(BmpInfoHeader, Marshal.SizeOf(BmpInfoHeader)));

                //调试板信息
                for (ColorIndex = 0; ColorIndex < m_nColorTableEntries; ColorIndex++)
                {
                    ColorMask[ColorIndex].redmask = (byte)ColorIndex;
                    ColorMask[ColorIndex].greenmask = (byte)ColorIndex;
                    ColorMask[ColorIndex].bluemask = (byte)ColorIndex;
                    ColorMask[ColorIndex].rgbReserved = 0;

                    TmpBinaryWriter.Write(StructToBytes(ColorMask[ColorIndex], Marshal.SizeOf(ColorMask[ColorIndex])));
                }

                //图片旋转，解决指纹图片倒立的问题
                RotatePic(buffer, nWidth, nHeight, ref ResBuf);

                //写图片
                //TmpBinaryWriter.Write(ResBuf);
                byte[] filter = null;
                if (w - nWidth > 0)
                {
                    filter = new byte[w - nWidth];
                }
                for (int i = 0; i < nHeight; i++)
                {
                    TmpBinaryWriter.Write(ResBuf, i * nWidth, nWidth);
                    if (w - nWidth > 0)
                    {
                        TmpBinaryWriter.Write(ResBuf, 0, w - nWidth);
                    }
                }

                FileStream.Close();
                TmpBinaryWriter.Close();
            }
            catch
            {
                //ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
                //logger.Append();
            }
        }
    }
}
