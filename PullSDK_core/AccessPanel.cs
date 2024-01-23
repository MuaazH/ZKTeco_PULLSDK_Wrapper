using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PullSDK_core;

public class AccessPanel
{
    private const string FpTable = "templatev10";
    private const string UserTable = "user";
    private const string AuthTable = "userauthorize";
    private const string TimezoneTable = "timezone";
    private const string TransactionsTable = "transaction";

    private const int HugeBufferSize = 20 * 1024 * 1024;
    private const int LargeBufferSize = 1024 * 1024 * 2;

    [DllImport("plcommpro.dll", EntryPoint = "Connect")]
    private static extern IntPtr Connect(string parameters);

    [DllImport("plcommpro.dll", EntryPoint = "Disconnect")]
    private static extern void Disconnect(IntPtr handle);

    [DllImport("plcommpro.dll", EntryPoint = "PullLastError")]
    private static extern int PullLastError();

    [DllImport("plcommpro.dll", EntryPoint = "GetDeviceData")]
    private static extern int GetDeviceData(IntPtr handle, ref byte buffer, int len, string table, string fieldNames,
        string filter, string options);

    [DllImport("plcommpro.dll", EntryPoint = "SetDeviceData")]
    private static extern int SetDeviceData(IntPtr handle, string table, byte[] data, string options);

    [DllImport("plcommpro.dll", EntryPoint = "DeleteDeviceData")]
    private static extern int DeleteDeviceData(IntPtr handle, string table, string data, string options);

    [DllImport("plcommpro.dll", EntryPoint = "ControlDevice")]
    private static extern int ControlDevice(IntPtr handle, int operation, int p1, int p2, int p3, int p4,
        string options);

    [DllImport("plcommpro.dll", EntryPoint = "GetDeviceParam")]
    private static extern int GetDeviceParam(IntPtr handle, ref byte buffer, int len, string item);

    [DllImport("plcommpro.dll", EntryPoint = "GetRTLog")]
    private static extern int GetRTLog(IntPtr handle, ref byte buffer, int len);

    IntPtr _handle = IntPtr.Zero;
    int _failCount;


    /**
     * returns the last error code
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public int GetLastError()
    {
        return PullLastError();
    }

    /**
     * Checks if the device is connected
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool IsConnected()
    {
        if (_handle != IntPtr.Zero)
        {
            if (_failCount > 5)
            {
                _failCount = 0;
                Disconnect();
                return false;
            }

            return true;
        }

        return _handle != IntPtr.Zero;
    }

    /**
     * Disconnects from shit
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Disconnect()
    {
        if (IsConnected())
        {
            Disconnect(_handle);
            _handle = IntPtr.Zero;
        }
    }

    /**
     * Connects to a device using the TCP protocol
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool Connect(string ip, int port, int key, int timeout)
    {
        if (IsConnected())
        {
            return false;
        }

        string connStr =
            $"protocol=TCP,ipaddress={ip},port={port},timeout={timeout},passwd={(key == 0 ? "" : key.ToString())}";
        _handle = Connect(connStr);
        return _handle != IntPtr.Zero;
    }

    /**
     * Reads a fingerprint from the device
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Fingerprint? GetFingerprint(string pin, int finger)
    {
        if (IsConnected())
        {
            byte[] buffer = new byte[LargeBufferSize];
            int readResult = GetDeviceData(_handle, ref buffer[0], buffer.Length, FpTable,
                "Size\tPin\tFingerID\tValid\tTemplate\tEndTag", // fieldNames,
                "Pin=" + pin + ",FingerID=" + finger + ",Valid=1", "");
            if (readResult >= 0)
            {
                int len = 0;
                while (len < buffer.Length && buffer[len] != 0)
                {
                    len++;
                }

                FpReader reader = new FpReader(Encoding.ASCII.GetString(buffer, 0, len));
                if (reader.ReadHead())
                {
                    int count = reader.LineCount;
                    for (int i = 0; i < count; i++)
                    {
                        Fingerprint? f = reader.Next();
                        if (f != null && f.FingerId == finger)
                        {
                            return f;
                        }
                    }

                    return new Fingerprint(pin, finger, null, null);
                }
            }
            else
            {
                _failCount++;
                return null;
            }
        }

        return null;
    }

    /**
     * Reads the doors that a user is allowed to access
     * returns null of failure
    */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public int ReadDoors(string pin, int timezone)
    {
        if (IsConnected())
        {
            byte[] buffer = new byte[HugeBufferSize];
            int readResult = GetDeviceData(_handle, ref buffer[0], buffer.Length, AuthTable,
                "Pin\tAuthorizeTimezoneId\tAuthorizeDoorId", $"AuthorizeTimezoneId={timezone},Pin={pin}", "");
            if (readResult >= 0)
            {
                int len = 0;
                while (len < buffer.Length && buffer[len] != 0)
                {
                    len++;
                }

                UserAuthReader reader = new UserAuthReader(Encoding.ASCII.GetString(buffer, 0, len));
                if (reader.ReadHead())
                {
                    UserAuthorization? a = reader.Next();
                    if (a == null)
                    {
                        throw new Exception("Could not parse auth data");
                    }

                    return a.Doors;
                }
            }
            else
            {
                _failCount++;
                return -1;
            }
        }

        return -1;
    }


    /**
     * Reads the doors that users are allowed to access
     */
    bool ReadDoors(List<User> users, int timezone)
    {
        if (IsConnected())
        {
            byte[] buffer = new byte[HugeBufferSize];
            int readResult = GetDeviceData(_handle, ref buffer[0], buffer.Length, AuthTable,
                "Pin\tAuthorizeTimezoneId\tAuthorizeDoorId", $"AuthorizeTimezoneId={timezone}", "");
            if (readResult >= 0)
            {
                int len = 0;
                while (len < buffer.Length && buffer[len] != 0)
                {
                    len++;
                }

                UserAuthReader reader = new UserAuthReader(Encoding.ASCII.GetString(buffer, 0, len));
                if (reader.ReadHead())
                {
                    int count = reader.LineCount;
                    for (int i = 0; i < count; i++)
                    {
                        UserAuthorization? a = reader.Next();
                        if (a == null)
                        {
                            throw new Exception("Could not parse auth data");
                        }

                        User tmp = new User(a.Pin, "", "", "", "", "");
                        int idx = users.BinarySearch(tmp);

                        if (idx >= 0)
                        {
                            users[idx].SetDoorsByFlag(a.Doors);
                        }
                    }

                    return true;
                }
            }
            else
            {
                _failCount++;
                return false;
            }
        }

        return false;
    }

    /**
     * Reads the fingerprints of users (A bit costly)
     */
    bool ReadFingerprints(List<User> users)
    {
        if (IsConnected())
        {
            byte[] buffer = new byte[HugeBufferSize];
            int readResult = GetDeviceData(_handle, ref buffer[0], buffer.Length, FpTable,
                //"Size\tPin\tFingerID\tValid\tTemplate\tEndTag",        // fieldNames,
                "Size\tPin\tFingerID\tValid\tEndTag", // fieldNames,
                "Valid=1", "");
            if (readResult >= 0)
            {
                int len = 0;
                while (len < buffer.Length && buffer[len] != 0)
                {
                    len++;
                }

                FpReader reader = new FpReader(Encoding.ASCII.GetString(buffer, 0, len));
                if (reader.ReadHead())
                {
                    int count = reader.LineCount;
                    for (int i = 0; i < count; i++)
                    {
                        Fingerprint? f = reader.Next();
                        if (f == null)
                        {
                            throw new Exception("Could not parse fingerprints");
                        }

                        User tmp = new User(f.Pin, "", "", "", "", "");
                        int idx = users.BinarySearch(tmp);
                        if (idx >= 0)
                        {
                            users[idx].AddFingerprint(f);
                        }
                        else
                        {
                            // A fingerprint without a user. S#!t! I mean poop.
                            // This should never happen
                        }
                    }

                    return true;
                }
            }
            else
            {
                _failCount++;
                return false;
            }
        }

        return false;
    }

    /**
     * reads the list of users from the device
     * Also reads fingerprints without templates
     * Also reads what doors these users are allowed on the default timezone (AuthorizeTimezoneId=1)
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public List<User>? ReadUsers()
    {
        if (IsConnected())
        {
            byte[] buffer = new byte[HugeBufferSize];
            if (GetDeviceData(_handle, ref buffer[0], buffer.Length, UserTable, "*", "", "") >= 0)
            {
                int len = 0;
                while (len < buffer.Length && buffer[len] != 0)
                {
                    len++;
                }

                UsersReader reader = new UsersReader(Encoding.ASCII.GetString(buffer, 0, len));
                if (reader.ReadHead())
                {
                    int count = reader.LineCount;
                    List<User> users = new List<User>();
                    if (count < 1)
                    {
                        return users;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        var usr = reader.Next();
                        if (usr == null)
                        {
                            throw new Exception("Could not parse users");
                        }

                        users.Add(usr);
                    }

                    users.Sort(); // sort by pin
                    if (!ReadFingerprints(users))
                    {
                        return null;
                    }

                    if (!ReadDoors(users, 1))
                    {
                        return null;
                    }

                    return users;
                }

                return null;
            }

            _failCount++;
            return null;
        }

        return null;
    }

    /**
     * reads device log
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Transaction[]? ReadTransactionLog(long startingTimestamp)
    {
        if (!IsConnected())
        {
            return null;
        }

        byte[] buffer = new byte[HugeBufferSize];
        if (GetDeviceData(_handle, ref buffer[0], buffer.Length, TransactionsTable, "*", "", "") < 0)
        {
            _failCount++;
            return null;
        }

        int len = 0;
        while (len < buffer.Length && buffer[len] != 0)
        {
            len++;
        }

        string txt = Encoding.ASCII.GetString(buffer, 0, len);
        //System.IO.File.AppendAllText("log_buffer", txt);
        TransactionReader reader = new TransactionReader(txt);
        if (!reader.ReadHead())
        {
            return null;
        }

        int maxCount = reader.LineCount;
        List<Transaction> transactions = new List<Transaction>(maxCount);
        for (int i = 0; i < maxCount; i++)
        {
            Transaction? t = reader.Next();
            if (t == null)
            {
                continue;
            }

            if (t.Timestamp >= startingTimestamp)
            {
                // if (t.IsAccessDenied() || t.IsAccessGranted())
                // {
                // if (!string.IsNullOrWhiteSpace(t.Card))
                // {
                transactions.Add(t);
                // }
                // }
            }
        }

        transactions.Sort();
        return transactions.ToArray();
    }

    /**
     * tests if the string "pass" is a valid password
     */
    public static bool IsPasswordValid(string pass)
    {
        if (pass.Length == 0)
        {
            return true;
        }

        foreach (char c in pass)
        {
            if (c < '0' || c > '9')
                return false;
        }

        return true;
    }

    /**
     * Tests if the "card" string is a valid card number
     */
    public static bool IsCardValid(string? card)
    {
        if (card == null) return false;
        if (card.Length == 0) return true;
        // this should return false when card > int.MaxValue
        return ulong.TryParse(card, out _);
    }

    public static bool IsPinValid(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            return false;
        }

        if (!ulong.TryParse(pin, out var x))
        {
            return false;
        }

        return x <= uint.MaxValue;
    }

    /**
     * writes a single user to the device
     * See <code>WriteUsers</code>
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool WriteUser(User u)
    {
        return WriteUsers(new[] { u });
    }


    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteUser(string pin)
    {
        if (!IsConnected())
        {
            return false;
        }

        if (!DeleteUserFingerprints(pin))
        {
            return false;
        }

        //if (!DeleteUserTimezones(pin)) {
        //return false;
        //}
        DeleteUserTimezones(pin); // ZKTeco is rubbish. May fail sometimes? Needs testing. I don't have a device.
        //DeleteUserTransactions(pin);
        if (0 <= DeleteDeviceData(_handle, UserTable, "Pin=" + pin, ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool SetUserDoors(string pin, int timezone, int[] doors)
    {
        if (0 > DeleteDeviceData(_handle, AuthTable, $"Pin={pin}", ""))
        {
            _failCount++;
            return false;
        }

        byte[] data = Encoding.ASCII.GetBytes(AuthTableData(pin, 1, doors));
        if (SetDeviceData(_handle, AuthTable, data, "") != 0)
        {
            _failCount++;
            return false;
        }

        return true;
    }

    /*public bool DeleteAllTransactions()
    {
        if (!IsConnected())
        {
            return false;
        }

        bool failed = false;
        DeleteDeviceData(handle, TRANSACTIONS_TABLE, "doorid=", "");
        for (int i = 0; i < 10; i++)
        {
            if (0 > DeleteDeviceData(handle, TRANSACTIONS_TABLE, "doorid=" + i, ""))
            {
                failed = true;
            }
        }

        if (failed)
        {
            failCount++;
        }

        return true;
    }*/

    /**
     * Deletes a user by card (Dangerous, ZKTeco dev's are idiots, No Foreign keys on the DB)
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteUserByCard(string card)
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, UserTable, "CardNo=" + card, ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    /**
     * Delete user fingerprints
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteUserFingerprints(string pin)
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, FpTable, "Pin=" + pin, ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    /**
     * removes a user's access to all doors
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteUserTimezones(string pin)
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, AuthTable, "Pin=" + pin, ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    // [MethodImpl(MethodImplOptions.Synchronized)]
    // public bool DeleteUserTransactions(string pin)
    // {
    //     if (!IsConnected())
    //     {
    //         return false;
    //     }
    //
    //     if (0 <= DeleteDeviceData(_handle, TransactionsTable, "pin=" + pin, ""))
    //     {
    //         return true;
    //     }
    //
    //     _failCount++;
    //     return false;
    // }

    /**
     * Cleans the device for a fresh start. (ZKTeco sucks)
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteAllDataFromDevice()
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, FpTable, "", "") && 0 <= DeleteDeviceData(_handle, UserTable, "", "") &&
            0 <= DeleteDeviceData(_handle, AuthTable, "", "") &&
            0 <= DeleteDeviceData(_handle, TimezoneTable, "", "") &&
            0 <= DeleteDeviceData(_handle, TransactionsTable, "", ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteAllFingerprints()
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, FpTable, "", ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteAllUsers()
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, UserTable, "", ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteAllUserAuth()
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, AuthTable, "", ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteAllTimezones()
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, TimezoneTable, "", ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteAllTransactions()
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, TransactionsTable, "", ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    /**
     * Deletes one fingerprint
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool DeleteFingerprint(string pin, int finger)
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= DeleteDeviceData(_handle, FpTable, "Pin=" + pin + ",FingerID=" + finger, ""))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    string TimezoneString(int id, int[] conf)
    {
        if (conf.Length != 21)
            throw new Exception("Invalid timezone length, A Timezone must be int[3*7]");
        return string.Format(
            "TimezoneId={21}" +
            "\tSunTime1={6}\tSunTime2={7}\tSunTime3={8}" +
            "\tMonTime1={9}\tMonTime2={10}\tMonTime3={11}" +
            "\tTueTime1={12}\tTueTime2={13}\tTueTime3={14}" +
            "\tWedTime1={15}\tWedTime2={16}\tWedTime3={17}" +
            "\tThuTime1={18}\tThuTime2={19}\tThuTime3={20}" +
            "\tFriTime1={0}\tFriTime2={1}\tFriTime3={2}" +
            "\tSatTime1={3}\tSatTime2={4}\tSatTime3={5}" +
            "\tHol1Time1=2359\tHol1Time2=0\tHol1Time3=0" +
            "\tHol2Time1=2359\tHol2Time2=0\tHol2Time3=0" +
            "\tHol3Time1=2359\tHol3Time2=0\tHol3Time3=0",
            conf[0], conf[1], conf[2],
            conf[3], conf[4], conf[5],
            conf[6], conf[7], conf[8],
            conf[9], conf[10], conf[11],
            conf[12], conf[13], conf[14],
            conf[15], conf[16], conf[17],
            conf[18], conf[19], conf[20],
            id
        );
    }

    /**
     * Writes a timezone to the device.
     * a timezone is an int[21] every 3 ints are 3 periods in the day
     * a period is a 32bit int, where the HIGH 16 bits are the start of the period, and the low 16 bits are the end
     * 16bits time format is hours*100+minutes
     * to select the entire day set the first period to <code>(0 << 16) | (2359) = 2359</code> and the 2nd & 3rd to zero
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool WriteTimezone(int id, int[] tz)
    {
        byte[] defaultTimeZoneData = Encoding.ASCII.GetBytes(TimezoneString(id, tz));
        if (SetDeviceData(_handle, TimezoneTable, defaultTimeZoneData, "") != 0)
        {
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private string AuthTableData(string pin, int timezoneId, int[]? doors)
    {
        //int[] allDoors = { 0, 1, 3, 7, 15, 31, 63, 127, 255, 1023 };
        int doorsCode = 0;
        if (doors != null)
            foreach (var t in doors)
            {
                try
                {
                    unchecked
                    {
                        doorsCode |= 1 << (t - 1);
                    }
                }
                catch
                {
                    // ignored
                }
            }

        return $"Pin={pin}\tAuthorizeTimezoneId={timezoneId}\tAuthorizeDoorId={doorsCode}";
    }

    /**
     * Writes users to the device and gives them access to the doors in the default timezone
     * also writes any valid fingerprints these users have
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool WriteUsers(User[] users)
    {
        if (!IsConnected() || users.Length == 0)
        {
            return false;
        }

        /*for (int i = 0; i < users.Length; i++) {
            User u = users[i];
            if (!IsPinValid(u.Pin) || !IsCardValid(u.Card))
            {
                return false;
            }
        }*/
        for (int k = 0; k < users.Length; k += 100)
        {
            StringBuilder sb = new StringBuilder();
            int end = Math.Min(k + 100, users.Length);
            for (int i = k; i < end; i++)
            {
                sb.Append(users[i]).Append("\r\n");
            }

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            if (SetDeviceData(_handle, UserTable, data, "") != 0)
            {
                _failCount++;
                return false;
            }
        }

        for (int k = 0; k < users.Length; k += 100)
        {
            StringBuilder sb = new StringBuilder();
            int end = Math.Min(k + 100, users.Length);
            for (int i = k; i < end; i++)
            {
                // only using default timezone (timezoneId = 1)
                sb.Append(AuthTableData(users[i].Pin, 1, users[i].Doors)).Append("\r\n");
            }

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            if (SetDeviceData(_handle, AuthTable, data, "") != 0)
            {
                _failCount++;
                return false;
            }
        }

        Fingerprint[] fingerprints =
            users.SelectMany(u => u.Fingerprints.Where(f => f.Template is { Length: > 100 })).ToArray();
        for (int k = 0; k < fingerprints.Length; k += 20)
        {
            StringBuilder sb = new StringBuilder();
            int end = Math.Min(k + 20, fingerprints.Length);
            for (int i = k; i < end; i++)
            {
                sb.Append(fingerprints[i]).Append("\r\n");
            }

            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            if (SetDeviceData(_handle, FpTable, data, "") != 0)
            {
                _failCount++;
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool WriteFingerprint(Fingerprint fp)
    {
        return WriteFingerprints(new[] { fp });
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool WriteFingerprints(Fingerprint[] fpList)
    {
        // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  !IsConnected() = {!IsConnected()}");
        // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  fpList.Length = {fpList.Length}");
        if (!IsConnected() || fpList.Length == 0)
        {
            return false;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < fpList.Length; i++)
        {
            // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  i = {i}");
            // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  !IsPinValid(fpList[i].Pin) = {!IsPinValid(fpList[i].Pin)}");
            // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  fpList[i].Template == null = {fpList[i].Template == null}");
            // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  fpList[i].Template?.Length < 100 = {fpList[i].Template?.Length < 100}");
            if (!IsPinValid(fpList[i].Pin) || fpList[i].Template == null || fpList[i].Template?.Length < 100)
            {
                // Console.WriteLine("         % pull sdk dbg % AddFingerprints()  returning false");
                return false;
            }

            sb.Append(fpList[i]).Append("\r\n");
        }

        string dataString = sb.ToString();
        // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  dataString = {dataString}");
        byte[] data = Encoding.ASCII.GetBytes(dataString);
        int err = SetDeviceData(_handle, FpTable, data, "");
        if (err == 0)
        {
            // Console.WriteLine("         % pull sdk dbg % AddFingerprints()  returning true");
            return true;
        }

        // Console.WriteLine($"         % pull sdk dbg % AddFingerprints()  GetLastError() = {GetLastError()}    err = {err}");
        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool StopAlarm()
    {
        if (!IsConnected())
        {
            return false;
        }

        if (0 <= ControlDevice(_handle, 2, // Operation id
                0, // null
                0, // null
                0, // null
                0, // Reserved,
                "" // Options
            ))
        {
            return true;
        }

        _failCount++;
        return false;
    }


    // first door is 1
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool OpenDoor(int doorId, int seconds)
    {
        if (!IsConnected())
        {
            return false;
        }

        // 255 seconds => open the door without time?
        if (seconds < 1 || (seconds > 60 && seconds != 255))
        {
            return false;
        }

        if (0 <= ControlDevice(_handle, 1, // Operation id
                doorId, // Door id
                1, // Output address
                seconds, // DoorState (in seconds)
                0, // Reserved,
                "" // Options
            ))
        {
            return true;
        }

        _failCount++;
        return false;
    }

    // first door is 1
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool CloseDoor(int doorId)
    {
        if (!IsConnected())
        {
            return false;
        }

        if (ControlDevice(_handle, 1, // Operation id
                doorId, // Door id
                1, // Output address
                0, // Disabled = closed?
                0, // Reserved,
                "" // Options
            ) >= 0)
        {
            return true;
        }

        _failCount++;
        return false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public int GetDoorCount()
    {
        if (!IsConnected())
        {
            return -1;
        }

        byte[] buffer = new byte[2048];
        if (-1 < GetDeviceParam(_handle, ref buffer[0], buffer.Length, "LockCount"))
        {
            string str = Encoding.ASCII.GetString(buffer).Trim().Replace("LockCount=", "");
            int count;
            if (int.TryParse(str, out count))
            {
                return count;
            }
        }
        else
        {
            _failCount++;
            return -1;
        }

        return -1;
    }

    string _lastEventTime = "0000-00-00 00:00:00";

    /**
     * Reads the latest events only
     */
    [MethodImpl(MethodImplOptions.Synchronized)]
    public AccessPanelEvent? GetEventLog()
    {
        if (IsConnected())
        {
            byte[] buf = new byte[LargeBufferSize];
            if (GetRTLog(_handle, ref buf[0], buf.Length) > -1)
            {
                string tempLastEventTime = _lastEventTime;
                string[] events = Encoding.ASCII.GetString(buf).Replace("\0", "").Trim()
                    .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                List<AccessPanelRtEvent> rtEvents = new List<AccessPanelRtEvent>();
                AccessPanelDoorsStatus? doorsStatus = null;
                for (int i = 0; i < events.Length; i++)
                {
                    if (events[i][0] == '\0')
                    {
                        continue;
                    }

                    string[] values = events[i].Split(new[] { ',' });
                    if (values.Length != 7)
                    {
                        continue;
                    }

                    if (String.Compare(values[0], tempLastEventTime, StringComparison.Ordinal) > 0)
                    {
                        tempLastEventTime = values[0];
                    }

                    _lastEventTime = values[0];
                    if (values[4].Equals("255"))
                    {
                        doorsStatus = new AccessPanelDoorsStatus(values[1], values[2]);
                    }
                    else
                    {
                        if (String.Compare(values[0], _lastEventTime, StringComparison.Ordinal) < 0)
                        {
                            continue;
                        }

                        AccessPanelRtEvent evt = new AccessPanelRtEvent(values[0], values[1], values[3],
                            int.Parse(values[4]), int.Parse(values[5]));
                        rtEvents.Add(evt);
                    }
                }

                _lastEventTime = tempLastEventTime;
                return new AccessPanelEvent(doorsStatus, rtEvents.ToArray());
            }
            else
            {
                _failCount++;
                return null;
            }
        }

        return null;
    }
}