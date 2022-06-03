namespace PullSDK_core;

public class UserAuthReader : CsvReader<UserAuthorization>
{
    int _pinIdx;
    int _zoneIdx;
    int _doorsIdx;

    public UserAuthReader(string buffer) : base(buffer)
    {
    }

    public override bool ReadHead()
    {
        _pinIdx = -1;
        _zoneIdx = -1;
        _doorsIdx = -1;
        string[]? head = NextLine();
        if (head == null) return false;
        for (int i = 0; i < head.Length; i++)
        {
            switch (head[i])
            {
                case "Pin":
                    _pinIdx = i;
                    break;
                case "AuthorizeTimezoneId":
                    _zoneIdx = i;
                    break;
                case "AuthorizeDoorId":
                    _doorsIdx = i;
                    break;
            }
        }

        return _pinIdx > -1 && _zoneIdx > -1 && _doorsIdx > -1;
    }

    public override UserAuthorization? Next()
    {
        string[]? line = NextLine();
        return line == null ? null : new UserAuthorization(line[_pinIdx], int.Parse(line[_zoneIdx]), int.Parse(line[_doorsIdx]));
    }
}