namespace PullSDK_core;

public abstract class CsvReader<T>
{
    //protected readonly string buffer;
    //protected int offset;
    protected string?[]? Lines;
    protected int Index;
    public readonly int LineCount;

    protected CsvReader(string buffer)
    {
        //this.buffer = buffer;
        //offset = 0;
        Lines = buffer.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
        Index = 0;
        LineCount = Lines.Length - 1;
    }

    protected string[]? NextLine()
    {
        if (Lines == null || Lines[Index] == null || Index >= Lines.Length) return null;
        string[]? result = Lines[Index]?.Split(new[] {','}, StringSplitOptions.None);
        // I must clear pointers i don't need
        // fingerprints can take up to megabytes of strings...
        Lines[Index] = null;
        Index++;
        return result;
    }

    public abstract bool ReadHead();

    public abstract T? Next();
}