using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Dalamud;
using Triggernometry.Core;


namespace Triggernometry.PluginBridges.BridgeNamazu;

/// <summary>
/// Wrapper for GreyMagic.MemoryBase
/// </summary>
public class GreyMagicMemoryBase
{
    public static void ExecuteWithLock(System.Action a) => ProxyPlugin.Framework.RunOnTick(a);
    public static T ExecuteWithLock<T>(Func<T> a) => ProxyPlugin.Framework.RunOnTick(a).Result;

    // Base class properties
    // public Process Process => _memory.Process;
    // public IntPtr ProcessHandle => _memory.ProcessHandle;
    // public bool IsProcessOpen => _memory.IsProcessOpen;
    // public IntPtr ImageBase => _memory.ImageBase;

    // Read
    // public byte[] ReadBytes<T>(IntPtr addr) where T : struct
    //     => _memory.ReadBytes<T>(addr);
    // public byte[] ReadBytes(IntPtr addr, int count, bool isRelative)
    //     => _memory.ReadBytes(addr, count, isRelative);
    public static byte[] ReadBytes(IntPtr addr, int count)
    {
        SafeMemory.ReadBytes(addr, count, out var buffer);
        return buffer;
    }

    // public T Read<T>(bool isRelative, params IntPtr[] addrs) where T : struct
    //     => _memory.Read<T>(isRelative, addrs);
    // public T Read<T>(IntPtr addr, bool isRelative) where T : struct
    //     => _memory.Read<T>(addr, isRelative);
    public static T Read<T>(IntPtr addr) where T : struct
    {
        SafeMemory.Read<T>(addr, out var res);
        return res;
    }

    // public T[] ReadArray<T>(IntPtr addr, int count, bool isRelative) where T : struct
    //     => _memory.ReadArray<T>(addr, count, isRelative);
    // public T[] ReadArray<T>(IntPtr addr, int count) where T : struct
    //     => _memory.ReadArray<T>(addr, count);
    //
    // public string ReadString(IntPtr address, Encoding encoding)
    //     => _memory.ReadString(address, encoding);
    // public string ReadString(IntPtr address, Encoding encoding, int maxLength)
    //     => _memory.ReadString(address, encoding, maxLength);
    // public string ReadString(IntPtr address, Encoding encoding, int maxLength, bool isRelative)
    //     => _memory.ReadString(address, encoding, maxLength, isRelative);
    // public string ReadStringUTF8(IntPtr address)
    //     => _memory.ReadStringUTF8(address);
    //
    // // Write
    // public int WriteBytes<T>(IntPtr addr, byte[] bytes, bool isRelative)
    //     => _memory.WriteBytes<T>(addr, bytes, isRelative);
    private static Lock WriteLock = new();

    public static void WriteBytes(IntPtr addr, byte[] bytes)
    {
        ExecuteWithLock(() =>
        {
            lock (WriteLock)
            {
                RealPlugin._instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"Writing MemoryBytes {addr} {ToMemoryView(bytes)}");
                SafeMemory.WriteBytes(addr, bytes);
            }
        });
    }
    [DllImport("kernel32.dll")]
    public static extern bool IsBadWritePtr(IntPtr lp, uint ucb);
    public static void Write<T>(IntPtr addr, T value) where T : struct
    {
        ExecuteWithLock(() =>
        {
            lock (WriteLock)
            {
                RealPlugin._instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"Writing Memory {addr} {value}");
                if (addr == IntPtr.Zero || IsBadWritePtr(addr, (uint)Marshal.SizeOf<T>()))
                    RealPlugin._instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"Bad Memory {addr} {value}");
                else SafeMemory.Write(addr, value);
            }
        });
    }

    public static string ToMemoryView(byte[] bytes, int bytesPerLine = 16)
    {
        if (bytes == null)
            return "byte[] is null";
        if (bytes.Length == 0)
            return "byte[] is empty";
        var result = new StringBuilder();
        int totalLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;

        for (int line = 0; line < totalLines; line++)
        {
            int startIndex = line * bytesPerLine;
            int endIndex = Math.Min(startIndex + bytesPerLine, bytes.Length);
            int currentLineByteCount = endIndex - startIndex;
            result.AppendFormat("{0:X8}  ", startIndex);
            for (int i = 0; i < bytesPerLine; i++)
            {
                if (i < currentLineByteCount) result.AppendFormat("{0:X2} ", bytes[startIndex + i]);
                else result.Append("   ");
            }
            result.Append(" ");
            for (int i = 0; i < currentLineByteCount; i++)
            {
                byte b = bytes[startIndex + i];
                char asciiChar = (b >= 32 && b <= 126) ? (char)b : '.';
                result.Append(asciiChar);
            }
            result.AppendLine();
        }
        return result.ToString();
    }
    // public void Write<T>(IntPtr addr, T value, bool isRelative) where T : struct
    //     => _memory.Write<T>(addr, value, isRelative);


    // public bool WriteString(IntPtr addr, string value, Encoding encoding)
    // {
    //     lock (Lock)
    //         return SafeMemory.WriteString(addr, value, encoding);
    // }

    // Allocate
    // public IntPtr AllocateMemory(int size)
    //     => _memory.AllocateMemory(size);
    //
    // public IntPtr AllocateMemory(int size, uint allocationType, uint protect)
    //     => _memory.AllocateMemory(size, allocationType, protect);
    //
    // public bool FreeMemory(IntPtr memory, int size, uint freeType)
    //     => _memory.FreeMemory(memory, size, freeType);
    //
    // public bool FreeMemory(IntPtr memory)
    //     => _memory.FreeMemory(memory);

    // Others
    // public IntPtr GetProcAddress(string module, string function)
    //     => _memory.GetProcAddress(module, function);
    //
    // public IntPtr GetVFTableEntry(IntPtr address, int index)
    //     => _memory.GetVFTableEntry(address, index);
    //
    // public IntPtr GetAbsolute(IntPtr relative)
    //     => _memory.GetAbsolute(relative);
    //
    // public IntPtr GetRelative(IntPtr absolute)
    //     => _memory.GetRelative(absolute);
}
