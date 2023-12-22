using System.Diagnostics;
using System.IO;

namespace CodeMarker;

internal class CodeMarkings
{
    private const string LineMarking = ">>>>>CM:";

    private static readonly HashSet<byte> _validChars = BuildValidChars();

    private static HashSet<byte> BuildValidChars()
    {
        // Taken from http://stackoverflow.com/questions/898669/how-can-i-detect-if-a-file-is-binary-non-text-in-python
        // With the addition of the 0 character.

        var result = new HashSet<byte>();

        result.Add(0); // UTF-16

        foreach (var c in new[] { 7, 8, 9, 10, 12, 13, 27 })
        {
            result.Add((byte)c);
        }

        for (int c = 0x20; c < 0x100; c++)
        {
            result.Add((byte)c);
        }

        return result;
    }

    private Offset[] _fileLines;
    private Offset[] _bufferLines;

    public string FileName { get; }
    public MarkColor?[] LineColors { get; set; }
    public string? Comments { get; set; }
    public Encoding Encoding { get; }
    public string LineTermination { get; }
    public string Text { get; }
    public ImmutableArray<MarkColor> MarkColors { get; private set; } =
        ImmutableArray<MarkColor>.Empty;

    public int FileLineCount => _fileLines.Length;

    private CodeMarkings(
        string fileName,
        MarkColor?[] lineColors,
        string? comments,
        Encoding encoding,
        string lineTermination,
        string text,
        Offset[] fileLines,
        Offset[] bufferLines
    )
    {
        FileName = fileName;
        LineColors = lineColors;
        Comments = comments;
        Encoding = encoding;
        LineTermination = lineTermination;
        Text = text;
        _fileLines = fileLines;
        _bufferLines = bufferLines;

        UpdatePrevalentColor();
    }

    public ImmutableArray<MarkColor> UpdatePrevalentColor()
    {
        var colors = LineColors
            .Where(p => p != null)
            .Cast<MarkColor>()
            .Distinct()
            .ToImmutableArray();

        if (colors.Length == 0)
            MarkColors = [MarkColor.Green];
        else
            MarkColors = colors;

        return MarkColors;
    }

    public static CodeMarkings? FromProjectItem(string fileName)
    {
        using var stream = File.OpenRead(fileName);

        var encoding = DetectEncoding(stream) ?? new UTF8Encoding(false);

        if (DetectIsBinary(stream))
            return null;

        string lineTermination = DetectLineTermination(stream);

        stream.Position = 0;

        var lines = GetLines(stream, encoding, lineTermination);
        var textLines = new List<string>();
        var commentLines = new List<string>();
        MarkColor? nextColor = null;
        bool inComments = false;
        var lineColors = new List<MarkColor?>();

        var bufferLines = new List<Offset>();
        var fileLines = new List<Offset>();

        int fileLine = 0;
        int bufferLine = -1;
        int bufferOffset = 0;

        foreach (var line in lines)
        {
            if (inComments)
            {
                commentLines.Add(line.Text);

                fileLines.Add(new Offset(bufferLine, bufferOffset));
            }
            else if (line.Text.StartsWith(LineMarking))
            {
                string marking = line.Text.Substring(LineMarking.Length);

                if (marking == "COMMENTS")
                {
                    inComments = true;

                    fileLines.Add(new Offset(bufferLine, bufferOffset));
                }
                else
                {
                    nextColor = MarkColor.GetColor(marking);

                    Debug.Assert(nextColor != null);

                    fileLines.Add(new Offset(bufferLine + 1, bufferOffset));
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(line.Text))
                    lineColors.Add(null);
                else
                    lineColors.Add(nextColor ?? MarkColor.Red);

                bufferLine++;

                nextColor = null;
                textLines.Add(line.Text);

                bufferLines.Add(new Offset(fileLine, line.Offset));
                fileLines.Add(new Offset(bufferLine, bufferOffset));

                bufferOffset += line.Text.Length + Environment.NewLine.Length;
            }

            fileLine++;
        }

        string? comments = null;

        if (commentLines.Count > 0)
            comments = string.Join(Environment.NewLine, commentLines);

        string text = string.Join(Environment.NewLine, textLines);

        return new CodeMarkings(
            fileName,
            lineColors.ToArray(),
            comments,
            encoding,
            lineTermination,
            text,
            fileLines.ToArray(),
            bufferLines.ToArray()
        );
    }

    public void Save()
    {
        var fileLines = new List<Offset>();
        var bufferLines = new List<Offset>();
        int fileLine = 0;
        int bufferLine = -1;
        int bufferOffset = 0;

        using (var stream = File.Create(FileName))
        using (var writer = new StreamWriter(stream, Encoding))
        {
            writer.Flush();

            int initialFileOffset = (int)stream.Position;

            string[] lines = Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                var color = LineColors[i];

                if (color != null && color != MarkColor.Red)
                {
                    writer.Write(LineMarking);
                    writer.Write(color.Name);
                    writer.Write(LineTermination);

                    fileLines.Add(new Offset(bufferLine, bufferOffset));
                    fileLine++;
                }

                writer.Flush();

                int fileOffset = (int)stream.Position - initialFileOffset;

                writer.Write(lines[i]);
                writer.Write(LineTermination);

                bufferLines.Add(new Offset(fileLine, fileOffset));
                fileLine++;
                bufferLine++;
                fileLines.Add(new Offset(bufferLine, bufferOffset));

                bufferOffset += lines[i].Length + Environment.NewLine.Length;
            }

            if (!String.IsNullOrEmpty(Comments))
            {
                writer.Write(LineMarking);
                writer.Write("COMMENTS");
                writer.Write(LineTermination);

                fileLines.Add(new Offset(bufferLine, bufferOffset));

                foreach (
                    string line in Comments.Split(
                        new[] { Environment.NewLine },
                        StringSplitOptions.None
                    )
                )
                {
                    writer.Write(line);
                    writer.Write(LineTermination);

                    fileLines.Add(new Offset(bufferLine, bufferOffset));
                }
            }
        }

        _fileLines = fileLines.ToArray();
        _bufferLines = bufferLines.ToArray();
    }

    public int LineToFile(int line)
    {
        return _bufferLines[line].Line;
    }

    public int LineFromFile(int line)
    {
        return _fileLines[line].Line;
    }

    public int PositionToFile(int position)
    {
        return TransformPosition(_fileLines, _bufferLines, position);
    }

    public int PositionFromFile(int position)
    {
        return TransformPosition(_bufferLines, _fileLines, position);
    }

    private int TransformPosition(Offset[] source, Offset[] target, int position)
    {
        if (source.Length == 0 || position < source[0].Index)
            return position;

        int index = source.Length - 1;

        for (int i = 0; i < source.Length - 1; i++)
        {
            if (source[i + 1].Index > position)
            {
                index = i;
                break;
            }
        }

        return target[source[index].Line].Index + (position - source[index].Index);
    }

    private static List<Line> GetLines(Stream stream, Encoding encoding, string lineEnding)
    {
        string content;

        using (var reader = new StreamReader(stream, encoding))
        {
            content = reader.ReadToEnd();
        }

        var result = new List<Line>();

        int offset = 0;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            bool lineEnd = false;
            int endLength = -1;

            if (c == '\r')
            {
                if (i < content.Length - 1 && content[i + 1] == '\n')
                    endLength = 2;
                else
                    endLength = 1;

                lineEnd = true;
            }
            else if (c == '\n')
            {
                endLength = 1;
                lineEnd = true;
            }

            if (lineEnd)
            {
                result.Add(new Line(content.Substring(offset, i - offset), offset));

                i += endLength - 1;
                offset = i + 1;
            }
        }

        if (offset != content.Length)
            result.Add(new Line(content.Substring(offset), offset));

        return result;
    }

    private static string DetectLineTermination(FileStream stream)
    {
        int c;

        while ((c = stream.ReadByte()) != -1)
        {
            if (c == 0)
                continue;

            if (c == '\r')
            {
                return stream.ReadByte() != '\n' ? "\r" : "\r\n";
            }

            if (c == '\n')
                return "\n";
        }

        // The default when we don't have any line ending.

        return "\r\n";
    }

    private static Encoding? DetectEncoding(FileStream stream)
    {
        if (stream.Length < 2)
            return null;

        var byteBuffer = new byte[Math.Min(stream.Length, 4)];
        int byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);

        if (byteBuffer[0] == 0xFE && byteBuffer[1] == 0xFF)
        {
            // Big Endian Unicode

            stream.Position = 2;
            return new UnicodeEncoding(true, true);
        }
        else if (byteBuffer[0] == 0xFF && byteBuffer[1] == 0xFE)
        {
            // Little Endian Unicode, or possibly little endian UTF32

            if (byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
            {
                stream.Position = 2;
                return new UnicodeEncoding(false, true);
            }
            else
            {
                stream.Position = 4;
                return new UTF32Encoding(false, true);
            }
        }
        else if (
            byteLen >= 3
            && byteBuffer[0] == 0xEF
            && byteBuffer[1] == 0xBB
            && byteBuffer[2] == 0xBF
        )
        {
            // UTF-8

            stream.Position = 3;
            return Encoding.UTF8;
        }
        else if (
            byteLen >= 4
            && byteBuffer[0] == 0
            && byteBuffer[1] == 0
            && byteBuffer[2] == 0xFE
            && byteBuffer[3] == 0xFF
        )
        {
            // Big Endian UTF32

            stream.Position = 4;
            return new UTF32Encoding(true, true);
        }

        stream.Position = 0;
        return null;
    }

    private static bool DetectIsBinary(Stream stream)
    {
        if (stream.Length == 0)
            return false;

        long position = stream.Position;

        var buffer = new byte[Math.Min(1024, stream.Length - stream.Position)];

        bool isBinary = false;
        int read = stream.Read(buffer, 0, buffer.Length);

        for (int i = 0; i < read; i++)
        {
            if (!_validChars.Contains(buffer[i]))
            {
                isBinary = true;
                break;
            }
        }

        stream.Position = position;

        return isBinary;
    }

    private record struct Offset(int Line, int Index);

    private record struct Line(string Text, int Offset);
}
