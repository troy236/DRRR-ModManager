using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RingRacersModManager.StackWalk;

public static class StackHelper {
    private const uint SYMOPT_UNDNAME = 0x02;
    private const uint SYMOPT_DEFERRED_LOADS = 0x04;
    private const uint SYMOPT_FAIL_CRITICAL_ERRORS = 0x200;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadLibrary(string lpLibFileName);

    [DllImport("dbghelp.dll")]
    private static extern uint SymSetOptions(uint SymOptions);
    [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SymInitialize(nint hProcess, string UserSearchPath, [MarshalAs(UnmanagedType.Bool)] bool fInvadeProcess);
    [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SymGetLineFromAddr64(nint hProcess, ulong qwAddr, ref uint pdwDisplacement, ref IMAGEHLP_LINE64 Line64);
    [DllImport("dbghelp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SymCleanup(nint hProcess);

    private static nint _dbgHelpPointer;

    public static void Initialize() {
#if DEBUG
        //Typically won't be running the debug build in AOT mode
        return;
#endif
        if (!OperatingSystem.IsWindows()) return;
        //TODO If anyone knows a library for reading DWARF file data or their format (clang .dbg and Mac xcode .dsym) please message me on Discord
        //Preferably with a working example to get source path/line number using the symbols provided
        _dbgHelpPointer = LoadLibrary("dbghelp.dll");
        if (_dbgHelpPointer == nint.Zero) return;
        SymSetOptions(SYMOPT_UNDNAME | SYMOPT_DEFERRED_LOADS | SYMOPT_FAIL_CRITICAL_ERRORS);
    }

    public static bool PrintAOTStackTrace(Exception exception) {
        //Uses dbghelp to parse Windows PDB for source info since Native AOT builds do not give this by default

#if DEBUG
        //Typically won't be running the debug build in AOT mode
        return false;
#endif
        //Check if running under Native AOT. This for some reason returns false if PublishAOT is enabled in the project even when not published
        if (RuntimeFeature.IsDynamicCodeSupported) return false;
        if (_dbgHelpPointer == nint.Zero) return false;
        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "DRRRModManager.pdb"))) return false;
        Process currentProcess = Process.GetCurrentProcess();
        if (!SymInitialize(currentProcess.Handle, null, true)) {
            return false;
        }
        Console.WriteLine($"{exception.GetType().ToString()}: {exception.Message}");
        Console.WriteLine();
        Console.WriteLine("Stack trace:");
        var stackTrace = new StackTrace(exception);
        var fileStream = new FileStream(Path.Combine(AppContext.BaseDirectory, "mmerror.txt"), FileMode.Create, FileAccess.Write, FileShare.None);
        fileStream.Write(Encoding.UTF8.GetBytes($"{exception.GetType().ToString()}: {exception.Message}"));
        fileStream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
        Exception currentException = exception;
        while (currentException != null) {
            foreach (var stackFrame in stackTrace.GetFrames()) {
                string stackFrameStr = stackFrame.ToString();
                string newstackFrameStr = stackFrameStr[..stackFrameStr.IndexOf(" + 0x")];
                nint ip = stackFrame.GetNativeIP();
                IMAGEHLP_LINE64 lineData = new();
                lineData.SizeOfStruct = (uint)Marshal.SizeOf<IMAGEHLP_LINE64>();
                uint ignore = 0;
                bool lineResult = SymGetLineFromAddr64(currentProcess.Handle, (ulong)ip, ref ignore, ref lineData);
                if (lineResult) {
                    string text = $"{newstackFrameStr} - {Marshal.PtrToStringAnsi(lineData.FileName)}:{lineData.LineNumber}";
                    Console.WriteLine(text);
                    fileStream.Write(Encoding.UTF8.GetBytes(text));
                }
                else {
                    //If Source data not found then just print what Native AOT gives us
                    string text = stackFrameStr[..stackFrameStr.IndexOf(" at offset")];
                    Console.WriteLine(text);
                    fileStream.Write(Encoding.UTF8.GetBytes(text));
                }
                fileStream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
            }
            //Move to next exception
            currentException = currentException.InnerException;
            if (currentException != null) {
                Console.WriteLine("Inner exception:");
                Console.WriteLine($"{currentException.GetType().ToString()}: {currentException.Message}");
                fileStream.Write(Encoding.UTF8.GetBytes("Inner exception:"));
                fileStream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
                fileStream.Write(Encoding.UTF8.GetBytes($"{currentException.GetType().ToString()}: {currentException.Message}"));
                fileStream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
            }
        }
        fileStream.Dispose();
        SymCleanup(currentProcess.Handle);
        return true;
    }
}