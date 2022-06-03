namespace PullSDK_core;

public class FpReader : CsvReader<Fingerprint>
{
    int _pinIndex;
    int _fidIndex;
    int _templateIndex;
    int _etagIndex;

    public FpReader(string buffer) : base(buffer)
    {
    }

    public override bool ReadHead()
    {
        _pinIndex = -1;
        _fidIndex = -1;
        _templateIndex = -1;
        _etagIndex = -1;
        string[]? head = NextLine();
        if (head == null) return false;
        for (int i = 0; i < head.Length; i++)
        {
            switch (head[i])
            {
                case "Pin":
                    _pinIndex = i;
                    break;
                case "FingerID":
                    _fidIndex = i;
                    break;
                case "Template":
                    _templateIndex = i;
                    break;
                case "EndTag":
                    _etagIndex = i;
                    break;
            }
        }

        return _etagIndex > -1 &&
//                templateIndex > -1 &&
               _pinIndex > -1 && _fidIndex > -1;
    }

    public override Fingerprint? Next()
    {
        string[]? line = NextLine();
        return line == null ? null : new Fingerprint(line[_pinIndex], int.Parse(line[_fidIndex]), _templateIndex > -1 ? line[_templateIndex] : null, line[_etagIndex]);
    }
}