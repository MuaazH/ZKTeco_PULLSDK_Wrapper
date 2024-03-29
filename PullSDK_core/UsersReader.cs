﻿namespace PullSDK_core;

public class UsersReader : CsvReader<User>
{
    int _cardIndex;
    int _nameIndex;
    int _startDateIndex;
    int _endDateIndex;
    int _passIndex;
    int _pinIndex;

    public UsersReader(string buffer) : base(buffer)
    {
    }

    public override bool ReadHead()
    {
        _nameIndex = -1;
        _startDateIndex = -1;
        _endDateIndex = -1;
        _passIndex = -1;
        _pinIndex = -1;
        string[]? head = NextLine();
        if (head == null) return false;
        for (int i = 0; i < head.Length; i++)
        {
            switch (head[i])
            {
                // CardNo,Pin,Password,Group,StartTime,EndTime,SuperAuthorize
                case "CardNo":
                    _cardIndex = i;
                    break;
                case "Pin":
                    _pinIndex = i;
                    break;
                case "Name":
                    _nameIndex = i; // old devices don't have this
                    break;
                case "Password":
                    _passIndex = i;
                    break;
                case "StartTime":
                    _startDateIndex = i;
                    break;
                case "EndTime":
                    _endDateIndex = i;
                    break;
            }
        }

        if (_cardIndex > -1 && _startDateIndex > -1 && _endDateIndex > -1 && _passIndex > -1 && _pinIndex > -1)
        {
            return true;
        }

        // Console.WriteLine("Bad header: " + string.Join(";", head));
        return false;
    }

    public override User? Next()
    {
        string[]? line = NextLine();
        return line == null
            ? null
            : new User(line[_pinIndex], _nameIndex > -1 ? line[_nameIndex] : "", line[_cardIndex], line[_passIndex],
                line[_startDateIndex],
                line[_endDateIndex]);
    }
}