using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace i04PullSDK
{

    public class Transaction: IComparable<Transaction> {
        public int VirificationMethod { get; set; }
        public string Card { get; set; }
        public string Pin { get; set; }
        public int Door { get; set; }
        public int Event { get; set; }
        public int InOutState { get; set; }
        public long Timestamp { get; set; }

        public static readonly int DeviceBootEvent = 206;

        static readonly int[] accessGrantedCodes = {
            00, // "Normal Punch Open"
            01, // "Punch during Normal Open Time Zone"
            02, // "First Card Normal Open"
            03, // "Multi-Card Open"
            04, // "Emergency Password Open"
            05, // "Open during Normal Open Time Zone"
            14, // "Press Fingerprint Open"
            15, // "Multi-Card Open"
            16, // "Press Fingerprint during Normal Open Time Zone"
            17, // "Card plus Fingerprint Open"
            18, // "First Card Normal Open"
            19, // "First Card Normal Open"
            26, // "Multi-Card Authentication"
            32  // "Multi-Card Authentication"
        };

        static readonly int[] accessDeniedCodes = {
            23, // "Access Denied"
            27, // "Unregistered Card"
            29, // "Card Expired"
            30, // "Password Error"
            33, // "Fingerprint Expired"
            34  // "Unregistered Fingerprint"
        };

        public Transaction(int virificationMethod,
                string card,
                string pin,
                int door,
                int eventType,
                int inOutState,
                long timestamp
        ) {
            VirificationMethod = virificationMethod;
            Card = card;
            Pin = pin;
            Door = door;
            Event = eventType;
            InOutState = inOutState;
            Timestamp = timestamp;
        }
        
        public bool IsAccessGranted() {
            return Array.BinarySearch(accessGrantedCodes, Event) > -1;
        }
        
        public bool IsAccessDenied() {
            return Array.BinarySearch(accessDeniedCodes, Event) > -1;
        }

        public bool IsEntry() {
            return InOutState == 0;
        }

        public bool IsExit() {
            return InOutState == 1;
        }
        
        #region IComparable implementation
        public int CompareTo(Transaction other)
        {
            if (Timestamp < other.Timestamp) return -1;
            if (Timestamp > other.Timestamp) return 1;
            if (Pin == null) {
                if (other.Pin == null) return 0;
                return -1;
            }
            return other.Pin == null ? 1 : Pin.CompareTo(other.Pin);
        }
        #endregion
    }
    
    public class Fingerprint : IComparable<Fingerprint>
    {

        public Fingerprint(string pin, int finger, string template, string endTag)
        {
            Pin = pin;
            FingerID = finger;
            Template = template;
            EndTag = endTag;
        }

        public string Pin { get; set; } // the FK that links users to their fingerprints
        public int FingerID { get; set; }
        public string Template { get; set; }
        public string EndTag { get; set; } // idk what this is... :\
        
        string NotNull(string s)
        {
            return s ?? "";
        }

        public string Json()
        {
            return string.Format(
                "{{\"i\":\"{0}\",\"f\":{1},\"t\":\"{2}\",\"e\":\"{3}\"}}",
                Pin,
                FingerID,
                Template,
                EndTag
            );
        }
        
        public override string ToString()
        {
            int size = Template == null ? 0 : Convert.FromBase64String(Template).Length;
            return "Size=" + size + "\tPin=" + NotNull(Pin) + "\tFingerID=" + FingerID + "\tValid=1\tTemplate="
                + NotNull(Template)
                + "\tEndTag=" + NotNull(EndTag);
        }

        #region IComparable implementation
        public int CompareTo(Fingerprint other)
        {
            // disable once StringCompareToIsCultureSpecific
            int c = NotNull(this.Pin).CompareTo(NotNull(other.Pin));
            return c == 0 ? FingerID.CompareTo(other.FingerID) : c;
        }
        #endregion
    }

    public class User : IComparable<User>
    {

        public User(string pin, string name, string card, string password, string startTime, string endTime)
        {
            Name = name;
            Password = password;
            StartTime = startTime;
            EndTime = endTime;
            Card = card;
            Pin = pin;
            Fingerprints = new Fingerprint[0];
        }

        public User(string pin, string name, string card, string password, string startTime, string endTime, int[] doors) : this(pin, name, card, password, startTime, endTime)
        {
            Doors = doors;
        }

        private int[] _doors = new int[0];
        public string Pin { get; set; }
        public string Card { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int[] Doors {
            get { return _doors; }
            set { _doors = value == null ? new int[0] : value; }
        }
        public Fingerprint[] Fingerprints { get; set; }

        #region IComparable implementation
        public int CompareTo(User other)
        {
            if (other == null) return 1;
            // disable once StringCompareToIsCultureSpecific
            return NotNull(this.Pin).CompareTo(NotNull(other.Pin));
        }
        #endregion

        public string Json()
        {
            // c => card
            // n => name
            // f => fromDate
            // t => toDate
            // p => Pin
            // s => Password
            return
                "{" +
                "\"p\":\"" + Pin + "\"," +
                "\"c\":\"" + Card + "\"," +
                "\"n\":\"" + Name + "\"," +
                "\"f\":\"" + StartTime + "\"," +
                "\"t\":\"" + EndTime + "\"," +
                "\"s\":\"" + Password + "\"" +
                "}";
                
        }
        
        public void RemoveFP(int fingerIndex) {
            List<Fingerprint> good = new List<Fingerprint>();
            for (int i = 0; i < Fingerprints.Length; i++) {
                if (Fingerprints[i].FingerID == fingerIndex) {
                    continue;
                }
                good.Add(Fingerprints[i]);
            }
            Fingerprints = good.ToArray();
        }

        public void AddFP(Fingerprint f)
        {
            if (Fingerprints.Length >= 10)
            {
                throw new IndexOutOfRangeException("A user can only have 10 fingerprints");
            }
            Fingerprint[] arr = new Fingerprint[Fingerprints.Length + 1];
            for (int i = 0; i < Fingerprints.Length; i++)
            {
                arr[i] = Fingerprints[i];
            }
            arr[Fingerprints.Length] = f;
            Fingerprints = arr;
        }

        public bool HasFingerprint(int finger)
        {
            if (Fingerprints != null)
            {
                for (int i = 0; i < Fingerprints.Length; i++)
                {
                    if (Fingerprints[i].FingerID == finger)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Equals(User that) {
            return
                object.Equals(this.Name, that.Name)
                && object.Equals(this.Password, that.Password)
                && object.Equals(this.Pin, that.Pin)
                && object.Equals(this.Card, that.Card)
                && object.Equals(this.StartTime, that.StartTime)
                && object.Equals(this.EndTime, that.EndTime);
        }

        string NotNull(string s)
        {
            return s ?? "";
        }
        
        public override string ToString()
        {
            /*
                    CardNo
                    Pin
                    Name
                    Password
                    StartTime
                    EndTime
             */
            return
                "CardNo=" + NotNull(Card) + "\t" +
                "Pin=" + NotNull(Pin) + "\t" +
                "Name=" + NotNull(Name) + "\t" +
                "Password=" + NotNull(Password) + "\t" +
                "StartTime=" + NotNull(StartTime) + "\t" +
                "EndTime=" + NotNull(EndTime);
        }
        
        public static string Json(User[] users)
        {
            StringBuilder sb = new StringBuilder(users.Length * 64);
            sb.Append("[");
            if (users.Length > 0)
            {
                sb.Append(users[0].Json());
            }
            for (int i = 1; i < users.Length; i++)
            {
                sb.Append(',').Append(users[i].Json());
            }
            return sb.Append("]").ToString();
        }
        
    }

    public class AccessPanel {
    
        //const string DEFAULT_TIMEZONE = "TimezoneId=1\tSunTime1=2359\tSunTime2=0\tSunTime3=0\tMonTime1=2359\tMonTime2=0\tMonTime3=0\tTueTime1=2359\tTueTime2=0\tTueTime3=0\tWedTime1=2359\tWedTime2=0\tWedTime3=0\tThuTime1=2359\tThuTime2=0\tThuTime3=0\tFriTime1=2359\tFriTime2=0\tFriTime3=0\tSatTime1=2359\tSatTime2=0\tSatTime3=0\tHol1Time1=2359\tHol1Time2=0\tHol1Time3=0\tHol2Time1=2359\tHol2Time2=0\tHol2Time3=0\tHol3Time1=2359\tHol3Time2=0\tHol3Time3=0";
        const string FP_TABLE = "templatev10";
        const string USER_TABLE = "user";
        const string AUTH_TABLE = "userauthorize";
        const string TIMEZONE_TABLE = "timezone";
        const string TRANSACTIONS_TABLE = "transaction";

        const int HugeBufferSize = 16 * 1024 * 1024;
        const int LargeBufferSize = 1024 * 1024 * 2;

        [DllImport("plcommpro.dll", EntryPoint = "Connect")]
        static extern IntPtr Connect(string parameters);

        [DllImport("plcommpro.dll", EntryPoint = "Disconnect")]
        static extern void Disconnect(IntPtr handle);

        [DllImport("plcommpro.dll", EntryPoint = "PullLastError")]
        static extern int PullLastError();

        [DllImport("plcommpro.dll", EntryPoint = "GetDeviceData")]
        static extern int GetDeviceData(IntPtr handle, ref byte buffer, int len, string table, string fieldNames, string filter, string options);

        [DllImport("plcommpro.dll", EntryPoint = "SetDeviceData")]
        static extern int SetDeviceData(IntPtr handle, string table, byte[] data, string options);

        [DllImport("plcommpro.dll", EntryPoint = "DeleteDeviceData")]
        static extern int DeleteDeviceData(IntPtr handle, string table, string data, string options);
        
        [DllImport("plcommpro.dll", EntryPoint = "ControlDevice")]
        static extern int ControlDevice(IntPtr handle, int operation, int p1, int p2, int p3, int p4, string options);

        [DllImport("plcommpro.dll", EntryPoint = "GetDeviceParam")]
        static extern int GetDeviceParam(IntPtr handle, ref byte buffer, int len, string item);

        [DllImport("plcommpro.dll", EntryPoint = "GetRTLog")]
        static extern int GetRTLog(IntPtr handle, ref byte buffer, int len);
        
        IntPtr handle = IntPtr.Zero;
        int failCount = 0;

        public int GetLastError() {
            return PullLastError();
        }
         
        public bool IsConnected() {
            if (handle != IntPtr.Zero) {
                if (failCount > 10) {
                    failCount = 0;
                    Disconnect();
                    return false;
                }
                return true;
            }
            return handle != IntPtr.Zero;
        }

        public void Disconnect() {
            if (IsConnected()) {
                Disconnect(handle);
                handle = IntPtr.Zero;
            }
        }

        public bool Connect(string ip, int port, int key, int timeout) {
            if (IsConnected()) {
                return false;
            }
            string connStr = "protocol=TCP,ipaddress=" + ip +
                    ",port=" + port +
                    ",timeout=" + timeout +
                ",passwd=" + (key == 0 ? "" : key.ToString());
            handle = Connect(connStr);
               return handle != IntPtr.Zero;
        }
        
        public Fingerprint GetFingerprint(string pin, int finger) {
            if (IsConnected()) {
                byte[] buffer = new byte[LargeBufferSize];
                int readResult = GetDeviceData(
                                        handle,
                                        ref buffer[0],
                                        buffer.Length,
                                        FP_TABLE,
                                        "Size\tPin\tFingerID\tValid\tTemplate\tEndTag",        // fieldNames,
                                        "Pin=" + pin + ",FingerID=" + finger + ",Valid=1",
                                        ""
                                );
                if (readResult >= 0) {
                    int len = 0;
                    while (len < buffer.Length && buffer[len] != 0) {
                        len++;
                    }
                    FPReader reader = new FPReader(Encoding.ASCII.GetString(buffer, 0, len));
                    buffer = null; // release memory
                    if (reader.ReadHead())
                    {
                        int count = reader.LineCount;
                           for (int i = 0; i < count; i++) {
                            Fingerprint f = reader.Next();
                            if (f.FingerID == finger) {
                                return f;
                            }
                        }
                        return new Fingerprint(pin, finger, null, null);
                    }
                } else {
                    failCount++;
                    return null;
                }
            }
            return null;
        }

        bool ReadFingerprints(List<User> users) {
            if (IsConnected()) {
                byte[] buffer = new byte[HugeBufferSize];
                int readResult = GetDeviceData(
                                        handle,
                                        ref buffer[0],
                                        buffer.Length,
                                        FP_TABLE,
                                        //"Size\tPin\tFingerID\tValid\tTemplate\tEndTag",        // fieldNames,
                                        "Size\tPin\tFingerID\tValid\tEndTag",        // fieldNames,
                                        "Valid=1",
                                        ""
                                );
                if (readResult >= 0) {
                    int len = 0;
                    while (len < buffer.Length && buffer[len] != 0) {
                        len++;
                    }
                    FPReader reader = new FPReader(Encoding.ASCII.GetString(buffer, 0, len));
                    buffer = null; // release memory
                    users.Sort(); // sort by pin
                    if (reader.ReadHead())
                    {
                        int count = reader.LineCount;
                        for (int i = 0; i < count; i++)
                        {
                            Fingerprint f = reader.Next();
                            if (f == null) {
                                throw new Exception("Could not parse fingerprints");
                            }
                            User tmp = new User(f.Pin, null, null, null, null, null);
                            int idx = users.BinarySearch(tmp);
                            if (idx >= 0) {
                                users[idx].AddFP(f);
                            } else {
                                // A fingerprint without a user. S#!t! I mean poop.
                                // This should never happen
                            }
                        }
                        return true;
                    }
                } else {
                    failCount++;
                    return false;
                }
            }
            return false;
        }

/*        public void ReadTimezone() {
            if (IsConnected()) {
                byte[] buffer = new byte[HugeBufferSize];
                if (GetDeviceData(handle, ref buffer[0], buffer.Length, TIMEZONE_TABLE, "*", "", "") >= 0) {
                    int len = 0;
                    while (len < buffer.Length && buffer[len] != 0) {
                        len++;
                    }
                    string str = Encoding.ASCII.GetString(buffer, 0, len);
                    File.WriteAllText(TIMEZONE_TABLE, str);
                } else {
                    failCount++;
                }
            }
        }
*/        

        public List<User> ReadUsers() {
            if (IsConnected()) {
                byte[] buffer = new byte[HugeBufferSize];
                if (GetDeviceData(handle, ref buffer[0], buffer.Length, USER_TABLE, "*", "", "") >= 0) {
                    int len = 0;
                    while (len < buffer.Length && buffer[len] != 0) {
                        len++;
                    }
                    UsersReader reader = new UsersReader(Encoding.ASCII.GetString(buffer, 0, len));
                    buffer = null; // release memory
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
                            users.Add(reader.Next());
                            if (users[i] == null) {
                                throw new Exception("Could not parse users");
                            }
                        }
                        if (ReadFingerprints(users)) {
                            return users;
                        }
                        return null;
                    } else {
                        return null;
                    }
                } else {
                    failCount++;
                    return null;
                }
            }
            return null;
        }

        public Transaction[] ReadTransactionLog(long startingTimestamp) {
            if (!IsConnected()) {
                return null;
            }
               byte[] buffer = new byte[HugeBufferSize];
               if (GetDeviceData(handle, ref buffer[0], buffer.Length, TRANSACTIONS_TABLE, "*", "", "") < 0) {
                   failCount++;
                   return null;
               }
               int len = 0;
               while (len < buffer.Length && buffer[len] != 0) {
                   len++;
               }
               string txt = Encoding.ASCII.GetString(buffer, 0, len);
               //System.IO.File.AppendAllText("log_buffer", txt);
               TransactionReader reader = new TransactionReader(txt);
               if (!reader.ReadHead()) {
                   return null;
               }
               int maxCount = reader.LineCount;
               List<Transaction> transactions = new List<Transaction>(maxCount);
               for (int i = 0; i < maxCount; i++) {
                   Transaction t = reader.Next();
                   if (t == null) {
                       continue;
                   }
                   if (t.Timestamp >= startingTimestamp) {
                       if (t.IsAccessDenied() || t.IsAccessGranted()) {
                           if (!string.IsNullOrWhiteSpace(t.Card)) {
                               transactions.Add(t);
                           }
                       }
                   }
               }
               transactions.Sort();
               return transactions.ToArray();
        }

        public static bool IsPasswordValid(string pass) {
            if (pass == null) return false;
            if (pass.Length == 0) {
                return true;
            }
            foreach (char c in pass)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }
        
        public static bool IsCardValid(string card) {
            if (card == null) return false;
            if (card.Length == 0) return true;
            ulong x;
               if (!ulong.TryParse(card, out x)) {
                   return false;
               }
               return true;
        }

        public static bool IsPinValid(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin)) {
                return false;
            }
               ulong x;
               if (!ulong.TryParse(pin, out x)) {
                   return false;
               }
               return x <= uint.MaxValue;
        }

        public bool WriteUser(User u) {
            return WriteUsers(new User[]{u});
        }

        public bool DeleteUser(string pin) {
            if (!IsConnected()) {
                return false;
            }
            if (!DeleteUserFingerprints(pin)) {
                return false;
            }
            //if (!DeleteUserTimezones(pin)) {
                //return false;
            //}
            DeleteUserTimezones(pin);
            //DeleteUserTransactions(pin);
            if (0 <= DeleteDeviceData(handle, USER_TABLE, "Pin=" + pin, "")) {
                return true;
            }
            failCount++;
            return false;
        }

        public bool DeleteAllTransactions()
        {
            if (!IsConnected())
            {
                return false;
            }
            bool failed = false;
            DeleteDeviceData(handle, TRANSACTIONS_TABLE, "doorid=", "");
            for (int i = 0; i < 10; i++)
            {
                if (0 > DeleteDeviceData(handle, TRANSACTIONS_TABLE, "doorid="+i, ""))
                {
                    failed = true;
                }
            }
            if (failed)
            {
                failCount++;
            }
            return true;
        }
        public bool DeleteUserByCard(string card) {
            if (!IsConnected()) {
                return false;
            }
            if (0 <= DeleteDeviceData(handle, USER_TABLE, "CardNo=" + card, "")) {
                return true;
            }
            failCount++;
            return false;
            /*
            byte[] buffer = new byte[1024*2];
            if (GetDeviceData(handle, ref buffer[0], buffer.Length, USER_TABLE, "*", "CardNo=" + card, "") >= 0) {
                int len = 0;
                while (len < buffer.Length && buffer[len] != 0) {
                    len++;
                }
                UsersReader reader = new UsersReader(Encoding.ASCII.GetString(buffer, 0, len));
                buffer = null; // release memory
                if (reader.ReadHead())
                {
                    int count = reader.LineCount;
                    List<User> users = new List<User>(count);
                    for (int i = 0; i < count; i++)
                    {
                        User u = reader.Next();
                        DeleteUser(u.Pin);
                    }
                    return true;
                }
                return false;
            }
            failCount++;
            return false;
            */
        }

        public bool DeleteUserFingerprints(string pin) {
            if (!IsConnected()) {
                return false;
            }
            if (0 <= DeleteDeviceData(handle, FP_TABLE, "Pin=" + pin, "")) {
                return true;
            }
            failCount++;
            return false;
        }

        public bool DeleteUserTimezones(string pin)
        {
            if (!IsConnected())
            {
                return false;
            }
            if (0 <= DeleteDeviceData(handle, AUTH_TABLE, "Pin=" + pin, ""))
            {
                return true;
            }
            failCount++;
            return false;
        }
        public bool DeleteUserTransactions(string pin)
        {
            if (!IsConnected())
            {
                return false;
            }
            if (0 <= DeleteDeviceData(handle, TRANSACTIONS_TABLE, "pin=" + pin, ""))
            {
                return true;
            }
            failCount++;
            return false;
        }

        public bool DeleteAllDataFromDevie()
        {
            if (!IsConnected())
            {
                return false;
            }
            if (0 <= DeleteDeviceData(handle, FP_TABLE, "", "")
                    && 0 <= DeleteDeviceData(handle, USER_TABLE, "", "")
                    && 0 <= DeleteDeviceData(handle, AUTH_TABLE, "", "")
                    && 0 <= DeleteDeviceData(handle, TIMEZONE_TABLE, "", "")
                    && 0 <= DeleteDeviceData(handle, TRANSACTIONS_TABLE, "", ""))
            {
                return true;
            }
            failCount++;
            return false;
        }

        public bool DeleteFingerprint(string pin, int finger) {
            if (!IsConnected()) {
                return false;
            }
            if (0 <= DeleteDeviceData(handle, FP_TABLE, "Pin=" + pin + ",FingerID=" + finger, "")) {
                return true;
            }
            failCount++;
            return false;
        }
        
        string TimezoneString(int[] conf) {
            return string.Format(
                "TimezoneId=1\tSunTime1={6}\tSunTime2={7}\tSunTime3={8}\tMonTime1={9}\tMonTime2={10}\tMonTime3={11}\tTueTime1={12}\tTueTime2={13}\tTueTime3={14}\tWedTime1={15}\tWedTime2={16}\tWedTime3={17}\tThuTime1={18}\tThuTime2={19}\tThuTime3={20}\tFriTime1={0}\tFriTime2={1}\tFriTime3={2}\tSatTime1={3}\tSatTime2={4}\tSatTime3={5}\tHol1Time1=2359\tHol1Time2=0\tHol1Time3=0\tHol2Time1=2359\tHol2Time2=0\tHol2Time3=0\tHol3Time1=2359\tHol3Time2=0\tHol3Time3=0",
                conf[0], conf[1], conf[2],
                conf[3], conf[4], conf[5],
                conf[6], conf[7], conf[8],
                conf[9], conf[10], conf[11],
                conf[12], conf[13], conf[14],
                conf[15], conf[16], conf[17],
                conf[18], conf[19], conf[20]
            );
        }
        
        public bool WriteTimezone(int[] tz) {
            byte[] defaultTimeZoneData = Encoding.ASCII.GetBytes(TimezoneString(tz));
            if (SetDeviceData(handle, TIMEZONE_TABLE, defaultTimeZoneData, "") != 0) {
                return false;
            }
            return true;
        }

        private string AuthTableData(string pin, int timezoneID, int[] doors)
        {

            //int[] allDoors = { 0, 1, 3, 7, 15, 31, 63, 127, 255, 1023 };
            int doorsCode = 0;
            if (doors != null)
                for (int i = 0; i < doors.Length; i++)
                {
                    try
                    {
                        unchecked
                        {
                            doorsCode |= 1 << (doors[i] - 1);
                        }
                    } catch {}
                }
            return
                "Pin=" + pin + "\t" +
                "AuthorizeTimezoneId=" + timezoneID + "\t" +
                "AuthorizeDoorId=" + doorsCode;
        }

        public bool WriteUsers(User[] users) {
              if (!IsConnected() || users.Length == 0) {
                return false;
            }
            /*for (int i = 0; i < users.Length; i++) {
                User u = users[i];
                if (!IsPinValid(u.Pin) || !IsCardValid(u.Card))
                {
                    return false;
                }
            }*/
            for (int k = 0; k < users.Length; k += 100) {
                StringBuilder sb = new StringBuilder();
                int end = Math.Min(k + 100, users.Length);
                for (int i = k; i < end; i++) {
                    sb.Append(users[i].ToString()).Append("\r\n");
                }
                byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
                if (SetDeviceData(handle, USER_TABLE, data, "") != 0) {
                    failCount++;
                    return false;
                }
            }

            for (int k = 0; k < users.Length; k += 100) {
                StringBuilder sb = new StringBuilder();
                int end = Math.Min(k + 100, users.Length);
                for (int i = k; i < end; i++) {
                    // only using default timezone
                    sb.Append(AuthTableData(users[i].Pin, 1, users[i].Doors)).Append("\r\n");
                }
                byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
                if (SetDeviceData(handle, AUTH_TABLE, data, "") != 0) {
                    failCount++;
                    return false;
                }
            }
            return true;
        }

        public bool AddFingerprint(Fingerprint fp) {
            return AddFingerprints(new Fingerprint[] { fp });
        }
    
        public bool AddFingerprints(Fingerprint[] fpList)
        {
              if (!IsConnected() || fpList.Length == 0) {
                return false;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fpList.Length; i++) {
                if (!IsPinValid(fpList[i].Pin) || fpList[i].Template == null || fpList[i].Template.Length < 100)
                {
                    return false;
                }
                sb.Append(fpList[i].ToString()).Append("\r\n");
            }
            byte[] data = Encoding.ASCII.GetBytes(sb.ToString());
            if (SetDeviceData(handle, FP_TABLE, data, "") == 0) {
                return true;
            }
            failCount++;
            return false;
        }

        public bool StopAlarm() {
            if (!IsConnected()) {
                return false;
            }
            if (0 <= ControlDevice(
                handle,
                2,  // Operation id
                0,  // null
                0,  // null
                0,  // null
                0,  // Reserved,
                ""  // Options
            )) {
                return true;
            }
            failCount++;
            return false;
        }
        
           // first door is 1
        public bool OpenDoor(int doorId, int seconds) {
            if (!IsConnected() || seconds < 1 || seconds > 60) {
                return false;
            }
            if (0 <= ControlDevice(
                handle,
                1,       // Operation id
                doorId,  // Door id
                1,       // Output address
                seconds, // DoorState (in seconds)
                0,       // Reserved,
                ""       // Options
            )) {
                return true;
            }
            failCount++;
            return false;
        }
        
           // first door is 1
        public bool CloseDoor(int doorId) {
            if (!IsConnected()) {
                return false;
            }
            if (ControlDevice(
                handle,
                1,       // Operation id
                doorId,  // Door id
                1,       // Output address
                0,       // Disabled = closed?
                0,       // Reserved,
                ""       // Options
            ) >= 0) {
                return true;
            }
            failCount++;
            return false;
        }

        public int GetDoorCount() {
            if (!IsConnected()) {
                return -1;
            }
            byte[] buffer = new byte[2048];
            if (-1 < GetDeviceParam(handle, ref buffer[0], buffer.Length, "LockCount")) {
                string str = Encoding.ASCII
                    .GetString(buffer)
                    .Trim()
                    .Replace("LockCount=", "");
                int count;
                if (int.TryParse(str, out count)) {
                    return count;
                }
            } else {
                failCount++;
                return -1;
            }
            return -1;
        }
        
        string lastEventTime = "0000-00-00 00:00:00";

        public AccessPanelEvent GetEventLog() {
            if (IsConnected()) {
                byte[] buf = new byte[LargeBufferSize];
                if (GetRTLog(handle, ref buf[0], buf.Length) > -1) {
                    string tempLastEventTime = lastEventTime;
                    string[] events = Encoding.ASCII.GetString(buf)
                        .Replace("\0", "")
                        .Trim()
                        .Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                    List<AccessPanelRTEvent> rtEvents = new List<AccessPanelRTEvent>();
                    AccessPanelDoorsStatus doorsStatus = null;
                    for (int i = 0; i < events.Length; i++) {
                        if (events[i][0] == '\0') {
                            continue;
                        }
                        string[] values = events[i].Split(new char[] {','});
                        if (values.Length != 7) {
                            continue;
                        }
                        if (values[0].CompareTo(tempLastEventTime) > 0) {
                            tempLastEventTime = values[0];
                        }
                        lastEventTime = values[0];
                        if (values[4].Equals("255")) {
                            doorsStatus = new AccessPanelDoorsStatus(values[1], values[2]);
                        } else {
                            if (values[0].CompareTo(lastEventTime) < 0) {
                                continue;
                            }
                            AccessPanelRTEvent evt = new AccessPanelRTEvent(values[0], values[1], values[3], int.Parse(values[4]), int.Parse(values[5]));
                            rtEvents.Add(evt);
                        }
                    }
                    lastEventTime = tempLastEventTime;
                    return new AccessPanelEvent(doorsStatus, rtEvents.ToArray());
                } else {
                    failCount++;
                    return null;
                }
            }
            return null;
        }

    }

    public class AccessPanelDoorsStatus {

        readonly int door;
        readonly int alarm;
        
        public AccessPanelDoorsStatus(string door, string alarm) {
            this.door = int.Parse(door);
            this.alarm = int.Parse(alarm);
        }
        
        public bool IsDoorClosed(int i) {
            return ((door >> (i*8)) & 255) == 1;
        }

        public bool IsDoorOpen(int i) {
            return ((door >> (i*8)) & 255) == 2;
        }

        public bool IsDoorSensorWorking(int i) {
            return ((door >> (i*8)) & 255) != 0;
        }

        public bool IsAlarmOn(int i) {
            return ((alarm >> (i*8)) & 255) != 0;
        }
        
        public string ToString(int i) {
            string a = (IsAlarmOn(i) ? ", ALARM!" : "");
            if (IsDoorClosed(i)) {
                return "Closed" + a;
            }
            if (IsDoorOpen(i)) {
                return "Open" + a;
            }
            if (!IsDoorSensorWorking(i)) {
                return "Sensor Not Working" + a;
            }
            return "Code " + ((door >> (i*8)) & 255) + a;
        }
    }

    public class AccessPanelRTEvent {
        public readonly string time;
        public readonly string pin;
        public readonly string door;
        public readonly int eventType;
        public readonly int inOrOut;
        
        public AccessPanelRTEvent(string time, string pin, string door, int eventType, int inOrOut)
        {
            this.time = time;
            this.pin = pin;
            this.door = door;
            this.eventType = eventType;
            this.inOrOut = inOrOut;
        }

        public int GetDoorID() {
            try {
                return !string.IsNullOrEmpty(door) ? int.Parse(door) : -1;
            } catch {
                return -1;
            }
        }
        
        public override string ToString()
        {
            string s = GetDescription(eventType);
            if (s == null) {
                s = "Unknown event";
            }
            if (!string.IsNullOrWhiteSpace(pin) && !"0".Equals(pin)) {
                s += ", " + (inOrOut == 0 ? "Entry" : (inOrOut == 1 ? "Exit" : "User")) + ": " + pin;
            }
            if (!string.IsNullOrWhiteSpace(door) && !"0".Equals(door)) {
                s += ", Door: " + door;
            }
            return "* Event: " + s;
        }

        public static string GetDescription(int code) {
            string[] e = {
                /*00*/ "Normal Punch Open",
                /*01*/ "Punch during Normal Open Time Zone",
                /*02*/ "First Card Normal Open",
                /*03*/ "Multi-Card Open",
                /*04*/ "Emergency Password Open",
                /*05*/ "Open during Normal Open Time Zone",
                /*06*/ "Linkage Event Triggered",
                /*07*/ "Alarm Canceled",
                /*08*/ "Remote Opening",
                /*09*/ "Remote Closing",
                /*10*/ "Disable Intraday Normal Open Time Zone",
                /*11*/ "Enable Intraday Normal Open Time Zone",
                /*12*/ "Open Auxiliary Output",
                /*13*/ "Close Auxiliary Output",
                /*14*/ "Press Fingerprint Open",
                /*15*/ "Multi-Card Open",
                /*16*/ "Press Fingerprint during Normal Open Time Zone",
                /*17*/ "Card plus Fingerprint Open",
                /*18*/ "First Card Normal Open",
                /*19*/ "First Card Normal Open",
                /*20*/ "Too Short Punch Interval",
                /*21*/ "Door Inactive Time Zone",
                /*22*/ "llegal Time Zone",
                /*23*/ "Access Denied",
                /*24*/ "Anti-Passback",
                /*25*/ "Interlock",
                /*26*/ "Multi-Card Authentication",
                /*27*/ "Unregistered Card",
                /*28*/ "Opening Timeout",
                /*29*/ "Card Expired",
                /*30*/ "Password Error",
                /*31*/ "Too Short Fingerprint Pressing Interval",
                /*32*/ "Multi-Card Authentication",
                /*33*/ "Fingerprint Expired",
                /*34*/ "Unregistered Fingerprint",
                /*35*/ "Door Inactive Time Zone",
                /*36*/ "Door Inactive Time Zone",
                /*37*/ "Failed to Close during Normal Open Time Zone",
            };
            if (code < 37 && code > -1) {
                return e[code];
            }
            switch (code) {
                case 101:
                    return "Duress Password Open";
                case 102:
                    return "Opened Accidentally";
                case 103:
                    return "Duress Fingerprint Open";
                case 200:
                    return "Door Opened Correctly";
                case 204:
                    return "Normal Open Time Zone Over";
                case 205:
                    return "Remote Normal Opening";
                case 206:
                    return "Device Start";
                case 220:
                    return "Auxiliary Input Disconnected";
                case 221:
                    return "Auxiliary Input Shorted";
            }
            return null;
        }
    }

    public class AccessPanelEvent {
        public readonly AccessPanelDoorsStatus DoorsStatus;
        public readonly AccessPanelRTEvent[] Events;
        
        public AccessPanelEvent(AccessPanelDoorsStatus DoorsStatus, AccessPanelRTEvent[] Events) {
            this.DoorsStatus = DoorsStatus;
            this.Events = Events;
        }
    }

    public abstract class CSVReader<T>
    {
        //protected readonly string buffer;
        //protected int offset;
        protected string[] lines;
        protected int index;
        public readonly int LineCount;

        protected CSVReader(string buffer)
        {
            //this.buffer = buffer;
            //offset = 0;
            lines = buffer.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            index = 0;
            LineCount = lines.Length - 1;
        }

        protected string[] NextLine()
        {
            if (lines == null || index >= lines.Length) return null;
            string[] result = lines[index].Split(new[] { ',' }, StringSplitOptions.None);
            // I must clear pointers i don't need
            // fingerprints can take up to megabytes of strings...
            lines[index] = null;
            index++;
            return result;
        }

        public abstract bool ReadHead();

        public abstract T Next();
    }

    public class FPReader : CSVReader<Fingerprint>
    {

        int pinIndex;
        int fidIndex;
        int templateIndex;
        int etagIndex;

        public FPReader(string buffer) : base(buffer) { }

        public override bool ReadHead()
        {
            pinIndex = -1;
            fidIndex = -1;
            templateIndex = -1;
            etagIndex = -1;
            string[] head = NextLine();
            if (head == null) return false;
            for (int i = 0; i < head.Length; i++)
            {
                switch (head[i])
                {
                    case "Pin":
                        pinIndex = i;
                        break;
                    case "FingerID":
                        fidIndex = i;
                        break;
                    case "Template":
                        templateIndex = i;
                        break;
                    case "EndTag":
                        etagIndex = i;
                        break;
                }
            }
            return
                etagIndex > -1 &&
//                templateIndex > -1 &&
                pinIndex > -1 &&
                fidIndex > -1;
        }

        public override Fingerprint Next()
        {
            string[] line = NextLine();
            return line == null
                ? null
                : new Fingerprint(
                        line[pinIndex],
                        int.Parse(line[fidIndex]),
                        templateIndex > -1 ? line[templateIndex] : null,
                        line[etagIndex]
                );
        }
    }

    public class UsersReader : CSVReader<User>
    {

        int cardIndex;
        int nameIndex;
        int startDateIndex;
        int endDateIndex;
        int passIndex;
        int pinIndex;

        public UsersReader(string buffer) : base(buffer)
        {
        }

        public override bool ReadHead()
        {
            nameIndex = -1;
            startDateIndex = -1;
            endDateIndex = -1;
            passIndex = -1;
            pinIndex = -1;
            string[] head = NextLine();
            if (head == null) return false;
            for (int i = 0; i < head.Length; i++)
            {
                switch (head[i])
                {
                    case "CardNo":
                        cardIndex = i;
                        break;
                    case "Pin":
                        pinIndex = i;
                        break;
                    case "Name":
                        nameIndex = i;
                        break;
                    case "Password":
                        passIndex = i;
                        break;
                    case "StartTime":
                        startDateIndex = i;
                        break;
                    case "EndTime":
                        endDateIndex = i;
                        break;
                }
            }
            return
                cardIndex > -1 &&
                nameIndex > -1 &&
                startDateIndex > -1 &&
                endDateIndex > -1 &&
                passIndex > -1 &&
                pinIndex > -1;
        }

        public override User Next()
        {
            string[] line = NextLine();
            return line == null
                ? null
                : new User(
                    line[pinIndex],
                    line[nameIndex],
                    line[cardIndex],
                    line[passIndex],
                    line[startDateIndex],
                    line[endDateIndex]
                );
        }
    }

    public class TransactionReader : CSVReader<Transaction> {
        public int vmIndex;
        public int cardIndex;
        public int pinIndex;
        public int doorIndex;
        public int eventIndex;
        public int inOutStateIndex;
        public int timestampIndex;
        
        public TransactionReader(string buffer) : base(buffer)
        {
        }

        public override bool ReadHead()
        {
            vmIndex = -1;
            cardIndex = -1;
            pinIndex = -1;
            doorIndex = -1;
            eventIndex = -1;
            inOutStateIndex = -1;
            timestampIndex = -1;
            // Cardno, Pin, Verified, DoorID, EventType, InOutState, Time_second
            string[] head = NextLine();
            if (head == null) return false;
            for (int i = 0; i < head.Length; i++)
            {
                switch (head[i].ToLower())
                {
                    case "cardno":
                        cardIndex = i;
                        break;
                    case "pin":
                        pinIndex = i;
                        break;
                    case "verified":
                        vmIndex = i;
                        break;
                    case "doorid":
                        doorIndex = i;
                        break;
                    case "eventtype":
                        eventIndex = i;
                        break;
                    case "inoutstate":
                        inOutStateIndex = i;
                        break;
                    case "time_second":
                        timestampIndex = i;
                        break;
                }
            }
            return  vmIndex > -1 &&
                    cardIndex > -1 &&
                    pinIndex > -1 &&
                    doorIndex > -1 &&
                    eventIndex > -1 &&
                    inOutStateIndex > -1 &&
                    timestampIndex > -1;
        }
        
        static readonly DateTime T0 = new DateTime(1970, 1, 1, 0, 0, 0);
        
        long ToEpoch(DateTime t) {
            return (long) (t - T0).TotalSeconds;
        }

        long ToTimestamp(long timeCoded) {
            try {
                long t = timeCoded;
                int second = (int) (t % 60);
                t /= 60L;
                int minute = (int) (t % 60);
                t /= 60L;
                int hour = (int) (t % 24);
                t /= 24L;
                int day = 1 + (int) (t % 31);
                t /= 31L;
                int month = 1 + (int) (t % 12);
                t /= 12;
                int year = (int) t + 2000;
                return ToEpoch(new DateTime(year, month, day, hour, minute, second));
            } catch {
                return 0;
                throw new Exception("Invalid timestamp " + timeCoded);
            }
        }
        
        public override Transaction Next()
        {
            string[] line = NextLine();
            return line == null
                ? null
                : new Transaction(
                    int.Parse(line[vmIndex]),
                    line[cardIndex],
                    line[pinIndex],
                    int.Parse(line[doorIndex]),
                    int.Parse(line[eventIndex]),
                    int.Parse(line[inOutStateIndex]),
                    ToTimestamp(long.Parse(line[timestampIndex]))
                );
        }
    }

}
