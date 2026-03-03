using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud;
using Triggernometry.PluginBridges.BridgeNamazu.Modules;
using Triggernometry.PScript;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx;

public abstract class Vfx {
    public IntPtr Ptr { get; set; }
    public string Path { get; set; }
    public string Tag { get; set; }
    public bool Removed { get; set; } = false;

    public ScriptUtils.IGBase? ImGuiObject;

    public const string DefaultTag = "Auto";
    public static VfxModule Module => BridgeNamazu.GetModule<VfxModule>();
    public static GreyMagicExternalProcessMemory Memory => BridgeNamazu.NamazuPlugin.Memory;
    public abstract bool TryRemove();

    public void ScheduleRemove(double duration) {
        if (duration > 0 && Ptr != IntPtr.Zero) {
            Task.Run(async () => {
                try {
                    await Task.Delay(TimeSpan.FromSeconds(duration)).ConfigureAwait(false);
                        GreyMagicMemoryBase.ExecuteWithLock(() => TryRemove());
                }
                catch (Exception ex) {
                    Module.ErrorLog($"[PictoACT] 延迟移除时出错：\n{ex}");
                }
            });
        }
    }

    public void Update() {
        if (Removed) return;
        Flag |= 0x2;
    }

    public byte Flag
    {
        get
        {
            SafeMemory.Read<byte>(Ptr + 0x38, out var flag);
            return flag;
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x38, value);
        }
    }

    public Vector3 Pos
    {
        get
        {
            SafeMemory.Read<Vector3>(Ptr + 0x50, out var raw);
            return new Vector3(raw.X, raw.Z, raw.Y);
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x50, new Vector3(value.X, value.Z, value.Y));
        }
    }

    public float Angle
    {
        get => Angles.X;
        set => Angles = new Vector3(value, 0, 0);
    }

    public Vector3 Angles
    {
        get
        {
            SafeMemory.Read<Vector4>(Ptr + 0x60, out var raw);
            var q = new Quaternion(raw.X, raw.Z, raw.Y, raw.W);

            float yaw;

            // pitch (x‑axis rotation = θx)
            var sinp = 2f * (q.W * q.X + q.Y * q.Z);
            var cosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
            var pitch = (float)Math.Atan2(sinp, cosp);

            // yaw (y‑axis rotation = θy)
            var siny = 2f * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(siny) >= 1f)
                yaw = (float)(Math.PI / 2 * Math.Sign(siny));
            else
                yaw = (float)Math.Asin(siny);

            // roll (z‑axis rotation = θ)
            var sinr = 2f * (q.W * q.Z + q.X * q.Y);
            var cosr = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            var roll = (float)Math.Atan2(sinr, cosr);

            return new Vector3(roll, pitch, yaw);
        }
        set
        {
            if (Removed) return;
            var q = Quaternion.CreateFromYawPitchRoll(value.Z, value.Y, value.X); // θy, θx, θ
            SafeMemory.Write(Ptr + 0x60, new Vector4(q.X, q.Z, q.Y, q.W));
        }
    }

    public Vector3 Scales
    {
        get
        {
            SafeMemory.Read<Vector3>(Ptr + 0x70, out var raw);
            return new Vector3(raw.X, raw.Z, raw.Y);
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x70, new Vector3(value.X, value.Z, value.Y));
        }
    }

    public uint ActorVfxSource
    {
        get
        {
            SafeMemory.Read<uint>(Ptr + 0x128, out var result);
            return result;
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x128, value);
        }
    }

    public uint ActorVfxTarget
    {
        get
        {
            SafeMemory.Read<uint>(Ptr + 0x130, out var res);
            return res;
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x130, value);
        }
    }

    public uint StaticVfxSource
    {
        get
        {
            SafeMemory.Read<uint>(Ptr + 0x1B8, out var res);
            return res;
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x1B8, value);
        }
    }

    public uint StaticVfxTarget
    {
        get
        {
            SafeMemory.Read<uint>(Ptr + 0x1C0, out var res);
            return res;
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x1C0, value);
        }
    }

    public float Speed
    {
        get
        {
            SafeMemory.Read<float>(Ptr + 0x250, out var res);
            return res;
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x250, value);
        }
    }

    public Vector4 Color
    {
        get
        {
            SafeMemory.Read<Vector4>(Ptr + 0x260, out var res);
            return res;
        }
        set
        {
            if (Removed) return;
            SafeMemory.Write(Ptr + 0x260, value);
        }
    }
}