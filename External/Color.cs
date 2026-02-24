public class Color
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;
    public bool IsAlphaEnabled;
    public bool IsAlphaVisible;

    public uint Rgba
    {
        get => (((uint)R) << 24) | (((uint)G) << 16) | (((uint)B) << 8) | A;
        set
        {
            R = (byte)((value >> 24) & 0xff);
            G = (byte)((value >> 16) & 0xff);
            B = (byte)((value >> 8) & 0xff);
            A = (byte)(value & 0xff);
        }
    }

    public uint Argb
    {
        get => (((uint)A) << 24) | (((uint)R) << 16) | (((uint)G) << 8) | B;
        set
        {
            A = (byte)((value >> 24) & 0xff);
            R = (byte)((value >> 16) & 0xff);
            G = (byte)((value >> 8) & 0xff);
            B = (byte)(value & 0xff);
        }
    }
}