using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using RingRacersModManager.UI;

namespace RingRacersModManager;
internal class ModLoader {

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
    nint hProcess,
    nint lpBaseAddress,
    [Out] byte[] lpBuffer,
    int dwSize,
    out nint lpNumberOfBytesRead);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
  nint hProcess,
  nint lpBaseAddress,
  byte[] lpBuffer,
  int nSize,
  out nint lpNumberOfBytesWritten);
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern nint VirtualAllocEx(nint hProcess, nint lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern bool VirtualFreeEx(nint hProcess, nint lpAddress, uint dwSize, uint dwFreeType);
    [DllImport("kernel32.dll", SetLastError = true)]

    private static extern nint CreateRemoteThread(nint hProcess,
   nint lpThreadAttributes, uint dwStackSize, nint lpStartAddress,
   nint lpParameter, uint dwCreationFlags, out nint lpThreadId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

    internal static async Task LoadAddons(Addon[] addons) {
        if (!OperatingSystem.IsWindows()) {
            Console.WriteLine("Modloader error: Not on Windows");
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Not running on Windows. Feature unavailable",
                    ButtonEnum.Ok, Icon.Error);
            var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;
            await box.ShowWindowDialogAsync(window);
            return;
        }
        var ringRacerProcesses = Process.GetProcessesByName("ringracers");
        if (ringRacerProcesses.Length == 0) {
            Console.WriteLine("Modloader error: Ring Racers is not running");
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Ring Racers is not running",
                    ButtonEnum.Ok, Icon.Error);
            var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;
            await box.ShowWindowDialogAsync(window);
            return;
        }
        byte[] gameMemory = new byte[ringRacerProcesses[0].MainModule.ModuleMemorySize];
        if (!ReadProcessMemory(
            ringRacerProcesses[0].Handle, ringRacerProcesses[0].MainModule.BaseAddress, gameMemory, ringRacerProcesses[0].MainModule.ModuleMemorySize, out _)) {

            Console.WriteLine("Modloader error: Failed to read game memory");
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Failed to read game memory",
                    ButtonEnum.Ok, Icon.Error);
            var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;
            await box.ShowWindowDialogAsync(window);
            return;
        }
        var functionOffset = FindAllBytes(gameMemory, [0x55, 0x57, 0x56, 0x53, 0x83, 0xec, 0x2c, 0x8b, 0x44, 0x24, 0x44, 0x89, 0x44, 0x24, 0x1c, 0x8b, 0x44, 0x24, 0x40, 0x89, 0x04, 0x24, 0xe8, 0x95, 0xb3, 0xfc, 0xff]);
        if (!functionOffset.Any()) {
            Console.WriteLine("Modloader error: Failed to find game function to load addons");
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Failed to find game function to load addons",
                    ButtonEnum.Ok, Icon.Error);
            var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;
            await box.ShowWindowDialogAsync(window);
            return;
        }
        StringBuilder addFileStr = new("addfile ");
        for (int i = 0; i < addons.Length; i++) {
            if (i != 0) addFileStr.Append(' ');
            addFileStr.Append(Path.GetFileName(addons[i].InstallPath));
        }
        byte[] addFileCommandBytes = Encoding.ASCII.GetBytes(addFileStr.ToString());
        nint argumentAddress = VirtualAllocEx(ringRacerProcesses[0].Handle, nint.Zero, (uint)addFileCommandBytes.Length, 0x1000 | 0x2000, 4);
        WriteProcessMemory(ringRacerProcesses[0].Handle, argumentAddress, addFileCommandBytes, addFileCommandBytes.Length, out nint bytesWritten);
        nint hThread = CreateRemoteThread(ringRacerProcesses[0].Handle, 0, 0, ringRacerProcesses[0].MainModule.BaseAddress + (nint)functionOffset.First(), argumentAddress, 0, out _);
        WaitForSingleObject(hThread, 0xFFFFFFFF);
        VirtualFreeEx(ringRacerProcesses[0].Handle, argumentAddress, 0, 0x8000);
        CloseHandle(hThread);
        Console.WriteLine($"Modloader: {addons.Length} addons loaded");
        var box2 = MessageBoxManager.GetMessageBoxStandard("", $"Successfully loaded {addons.Length} addons",
                ButtonEnum.Ok, Icon.Info);
        var window2 = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow;
        await box2.ShowWindowDialogAsync(window2);
    }

    private static int FindPattern(byte[] Body, byte[] Pattern, int start = 0) {
        int foundIndex = -1;
        bool match = false;

        if (Body.Length > 0
            && Pattern.Length > 0
            && start <= Body.Length - Pattern.Length && Pattern.Length <= Body.Length)
            for (int index = start; index <= Body.Length - Pattern.Length; index += 4)

                if (Body[index] == Pattern[0]) {
                    match = true;
                    for (int index2 = 1; index2 <= Pattern.Length - 1; index2++) {
                        if (Body[index + index2] != Pattern[index2]) {
                            match = false;
                            break;
                        }

                    }

                    if (match) {
                        foundIndex = index;
                        break;
                    }
                }

        return foundIndex;
    }

    private static IEnumerable<uint> FindAllBytes(byte[] bytes, byte[] signature, int alignment = 0) {
        var offset = 0;
        var ptrSize = 8;
        while (offset != -1) {
            offset = FindPattern(bytes, signature, offset);
            if (offset != -1) {
                yield return (uint)offset;
                offset += ptrSize;
            }
        }
    }
}
