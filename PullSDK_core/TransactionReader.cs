namespace PullSDK_core;

public class TransactionReader : CsvReader<Transaction>
{
    public int VmIndex;
    public int CardIndex;
    public int PinIndex;
    public int DoorIndex;
    public int EventIndex;
    public int InOutStateIndex;
    public int TimestampIndex;

    public TransactionReader(string buffer) : base(buffer)
    {
    }

    public override bool ReadHead()
    {
        VmIndex = -1;
        CardIndex = -1;
        PinIndex = -1;
        DoorIndex = -1;
        EventIndex = -1;
        InOutStateIndex = -1;
        TimestampIndex = -1;
        // Cardno, Pin, Verified, DoorID, EventType, InOutState, Time_second
        string[]? head = NextLine();
        if (head == null) return false;
        for (int i = 0; i < head.Length; i++)
        {
            switch (head[i].ToLower())
            {
                case "cardno":
                    CardIndex = i;
                    break;
                case "pin":
                    PinIndex = i;
                    break;
                case "verified":
                    VmIndex = i;
                    break;
                case "doorid":
                    DoorIndex = i;
                    break;
                case "eventtype":
                    EventIndex = i;
                    break;
                case "inoutstate":
                    InOutStateIndex = i;
                    break;
                case "time_second":
                    TimestampIndex = i;
                    break;
            }
        }

        return VmIndex > -1 && CardIndex > -1 && PinIndex > -1 && DoorIndex > -1 && EventIndex > -1 && InOutStateIndex > -1 && TimestampIndex > -1;
    }

    static readonly DateTime T0 = new DateTime(1970, 1, 1, 0, 0, 0);

    long ToEpoch(DateTime t)
    {
        return (long) (t - T0).TotalSeconds;
    }

    long ToTimestamp(long timeCoded)
    {
        try
        {
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
        }
        catch
        {
            return 0;
            // throw new Exception("Invalid timestamp " + timeCoded);
        }
    }

    public override Transaction? Next()
    {
        string[]? line = NextLine();
        return line == null ? null : new Transaction(int.Parse(line[VmIndex]), line[CardIndex], line[PinIndex], int.Parse(line[DoorIndex]), int.Parse(line[EventIndex]), int.Parse(line[InOutStateIndex]), ToTimestamp(long.Parse(line[TimestampIndex])));
    }
}