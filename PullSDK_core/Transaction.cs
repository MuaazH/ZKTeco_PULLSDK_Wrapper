namespace PullSDK_core;

public class Transaction : IComparable<Transaction>
{
    public int VerificationMethod { get; set; }
    public string Card { get; set; }
    public string Pin { get; set; }
    public int Door { get; set; }
    public int Event { get; set; }
    public int InOutState { get; set; }
    public long Timestamp { get; set; }

    public static readonly int DeviceBootEvent = 206;

    static readonly int[] AccessGrantedCodes =
    {
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
        32 // "Multi-Card Authentication"
    };

    static readonly int[] AccessDeniedCodes =
    {
        23, // "Access Denied"
        27, // "Unregistered Card"
        29, // "Card Expired"
        30, // "Password Error"
        33, // "Fingerprint Expired"
        34 // "Unregistered Fingerprint"
    };

    public Transaction(int verificationMethod, string card, string pin, int door, int eventType, int inOutState, long timestamp)
    {
        VerificationMethod = verificationMethod;
        Card = card;
        Pin = pin;
        Door = door;
        Event = eventType;
        InOutState = inOutState;
        Timestamp = timestamp;
    }

    public bool IsAccessGranted()
    {
        return Array.BinarySearch(AccessGrantedCodes, Event) > -1;
    }

    public bool IsAccessDenied()
    {
        return Array.BinarySearch(AccessDeniedCodes, Event) > -1;
    }

    public bool IsEntry()
    {
        return InOutState == 0;
    }

    public bool IsExit()
    {
        return InOutState == 1;
    }

    #region IComparable implementation

    public int CompareTo(Transaction? other)
    {
        if (other == null) return 1;
        if (Timestamp < other.Timestamp) return -1;
        if (Timestamp > other.Timestamp) return 1;
        return String.Compare(Pin, other.Pin, StringComparison.Ordinal);
    }

    #endregion
}