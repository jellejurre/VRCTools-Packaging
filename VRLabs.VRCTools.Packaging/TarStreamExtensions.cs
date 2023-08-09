using ICSharpCode.SharpZipLib.Tar;

namespace VRLabs.VRCTools.Packaging;

public static class TarStreamExtensions
{
    /// <summary>
    /// Reads and writes a file to the stream
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="source"></param>
    /// <param name="dest"></param>
    public static async Task WriteFileAsync(this TarOutputStream stream, string source, string dest)
    {
        await using Stream inputStream = File.OpenRead(source);
        long fileSize = inputStream.Length;
        var entry = TarEntry.CreateTarEntry(dest);
        entry.Size = fileSize;
        stream.PutNextEntry(entry);
        byte[] localBuffer = new byte[32 * 1024];
        while (true)
        {
            int numRead = await inputStream.ReadAsync(localBuffer);
            if (numRead <= 0)
                break;

            await stream.WriteAsync(localBuffer.AsMemory(0, numRead));
        }
        stream.CloseEntry();
    }

    /// <summary>
    /// Writes all text to the stream
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="dest"></param>
    /// <param name="content"></param>
    public static async Task WriteAllTextAsync(this TarOutputStream stream, string dest, string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);

        var entry = TarEntry.CreateTarEntry(dest);
        entry.Size = bytes.Length;
        stream.PutNextEntry(entry);
        await stream.WriteAsync(bytes);
        stream.CloseEntry();
    }

    /// <summary>
    /// Reads the next file in the stream
    /// </summary>
    /// <param name="tarIn"></param>
    /// <param name="outStream"></param>
    /// <returns></returns>
    public static async Task<long> ReadNextFileAsync(this TarInputStream tarIn, Stream outStream)
    {
        long totalRead = 0;
        byte[] buffer = new byte[4096];
        bool isAscii = true;
        bool cr = false;

        int numRead = await tarIn.ReadAsync(buffer);
        int maxCheck = Math.Min(200, numRead);

        totalRead += numRead;

        for (int i = 0; i < maxCheck; i++)
        {
            byte b = buffer[i];
            if (b is not (< 8 or > 13 and < 32 or 255)) continue;
            isAscii = false;
            break;
        }

        while (numRead > 0)
        {
            if (isAscii)
            {
                // Convert LF without CR to CRLF. Handle CRLF split over buffers.
                for (int i = 0; i < numRead; i++)
                {
                    byte b = buffer[i];     // assuming plain Ascii and not UTF-16
                    if (b == 10 && !cr)     // LF without CR
                        outStream.WriteByte(13);
                    cr = (b == 13);

                    outStream.WriteByte(b);
                }
            }
            else
                await outStream.WriteAsync(buffer.AsMemory(0, numRead));

            numRead = await tarIn.ReadAsync(buffer);
            totalRead += numRead;
        }

        return totalRead;
    }
}