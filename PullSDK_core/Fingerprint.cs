namespace PullSDK_core;

public class Fingerprint : IComparable<Fingerprint>
{
    public Fingerprint(string pin, int finger, string? template, string? endTag)
    {
        Pin = pin;
        FingerId = finger;
        Template = template;
        EndTag = endTag;
    }

    public string Pin { get; set; } // the FK that links users to their fingerprints
    public int FingerId { get; set; }
    public string? Template { get; set; }
    public string? EndTag { get; set; } // idk what this is... :\

    string NotNull(string? s)
    {
        return s ?? "";
    }

    public override string ToString()
    {
        int size = Template == null ? 0 : Convert.FromBase64String(Template).Length;
        return $"Size={size}\tPin={NotNull(Pin)}\tFingerID={FingerId}\tValid=1\tTemplate={NotNull(Template)}\tEndTag={NotNull(EndTag)}";
    }

    #region IComparable implementation

    public int CompareTo(Fingerprint? other)
    {
        if (other == null) return -1;
        int c = String.Compare(NotNull(this.Pin), NotNull(other.Pin), StringComparison.Ordinal);
        return c == 0 ? FingerId.CompareTo(other.FingerId) : c;
    }

    #endregion
}