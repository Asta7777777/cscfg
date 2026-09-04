namespace PhantomInstaller;

// Canonical UTF-8 bytes for Phantom CFG signatures.
// The generated payload uses Windows CRLF, while verification intentionally
// normalizes only the final payload newline. Canonicalizing the same final
// newline here keeps signatures stable without changing the CFG itself.
public static class Encoding
{
    public static PhantomUtf8Encoding UTF8 { get; } = new();
    public static System.Text.Encoding ASCII => System.Text.Encoding.ASCII;

    public sealed class PhantomUtf8Encoding
    {
        public byte[] GetBytes(string value)
        {
            if (value.EndsWith("\r\n", StringComparison.Ordinal))
                value = value[..^2] + "\n";
            return System.Text.Encoding.UTF8.GetBytes(value);
        }
    }
}
