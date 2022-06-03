namespace PullSDK_core;

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
        Fingerprints = Array.Empty<Fingerprint>();
    }

    public User(string pin, string name, string card, string password, string startTime, string endTime, int[] doors) : this(pin, name, card, password, startTime, endTime)
    {
        Doors = doors;
    }

    public string Pin { get; set; }
    public string Card { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }

    public int[] Doors { get; set; } = Array.Empty<int>();

    public void SetDoorsByFlag(int flag)
    {
        int count = 0;
        int[] buf = new int[16];
        for (int i = 0; i < 16; i++)
        {
            int bit = 1 << i;
            if ((flag & bit) != 0)
            {
                buf[count++] = i + 1;
            }
        }

        Doors = buf.Take(count).ToArray();
    }

    public Fingerprint[] Fingerprints { get; set; }

    #region IComparable implementation

    public int CompareTo(User? other)
    {
        if (other == null) return 1;
        return String.Compare(NotNull(Pin), NotNull(other.Pin), StringComparison.Ordinal);
    }

    #endregion

    public void RemoveFingerprint(int fingerIndex)
    {
        Fingerprints = Fingerprints.Where(t => t.FingerId != fingerIndex).ToArray();
    }

    public void AddFingerprint(Fingerprint f)
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
        for (int i = 0; i < Fingerprints.Length; i++)
        {
            if (Fingerprints[i].FingerId == finger)
            {
                return true;
            }
        }

        return false;
    }

    public bool Equals(User that)
    {
        return Equals(this.Name, that.Name) && Equals(this.Password, that.Password) && Equals(this.Pin, that.Pin) && Equals(this.Card, that.Card) && Equals(this.StartTime, that.StartTime) && Equals(this.EndTime, that.EndTime);
    }

    string NotNull(string? s)
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
        return $"CardNo={NotNull(Card)}\tPin={NotNull(Pin)}\tName={NotNull(Name)}\tPassword={NotNull(Password)}\tStartTime={NotNull(StartTime)}\tEndTime={NotNull(EndTime)}";
    }
}