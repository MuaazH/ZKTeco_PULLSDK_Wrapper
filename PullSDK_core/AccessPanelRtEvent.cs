namespace PullSDK_core;

public class AccessPanelRtEvent
{
    public readonly string Time;
    public readonly string Pin;
    public readonly string Door;
    public readonly int EventType;
    public readonly int InOrOut;

    public AccessPanelRtEvent(string time, string pin, string door, int eventType, int inOrOut)
    {
        this.Time = time;
        this.Pin = pin;
        this.Door = door;
        this.EventType = eventType;
        this.InOrOut = inOrOut;
    }

    public int GetDoorId()
    {
        try
        {
            return !string.IsNullOrEmpty(Door) ? int.Parse(Door) : -1;
        }
        catch
        {
            return -1;
        }
    }

    public override string ToString()
    {
        string? s = GetDescription(EventType);
        if (s == null)
        {
            s = "Unknown event";
        }

        if (!string.IsNullOrWhiteSpace(Pin) && !"0".Equals(Pin))
        {
            s += ", " + (InOrOut == 0 ? "Entry" : (InOrOut == 1 ? "Exit" : "User")) + ": " + Pin;
        }

        if (!string.IsNullOrWhiteSpace(Door) && !"0".Equals(Door))
        {
            s += ", Door: " + Door;
        }

        return "* Event: " + s;
    }

    public static string? GetDescription(int code)
    {
        string[] e =
        {
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
        if (code < 37 && code > -1)
        {
            return e[code];
        }

        switch (code)
        {
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