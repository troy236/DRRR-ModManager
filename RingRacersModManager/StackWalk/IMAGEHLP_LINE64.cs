using System.Runtime.InteropServices;

namespace RingRacersModManager.StackWalk;

[StructLayout(LayoutKind.Sequential)]
public struct IMAGEHLP_LINE64 {
    public uint SizeOfStruct;
    public nint Key;
    public uint LineNumber;
    public nint FileName;
    public ulong Address;
}