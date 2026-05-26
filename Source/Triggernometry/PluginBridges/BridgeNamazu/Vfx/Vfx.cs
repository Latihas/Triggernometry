using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Triggernometry.PluginBridges.BridgeNamazu.Modules;
using Triggernometry.PScript;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx;

public abstract class Vfx {
	public unsafe VfxObject* Ptr { get; set; }
	public string Path { get; set; }
	public string Tag { get; set; }
	public bool Removed { get; set; } = false;
        public DateTime? ExpireAtUtc { get; internal set; }

	public ScriptUtils.IGBase? ImGuiObject;

	public const string DefaultTag = "Auto";
	public static GreyMagicExternalProcessMemory Memory => BridgeNamazu.NamazuPlugin.Memory;
	public abstract bool TryRemove();

        public void ScheduleRemove(double duration)
            => VfxManager.ScheduleRemove(this, duration);

	public void Update() {
		if (Removed) return;
		Flag |= 0x2;
	}

	public unsafe byte Flag {
		get => (byte)Ptr->ObjectFlags;
		set {
			if (Removed) return;
			Ptr->ObjectFlags = value;
		}
	}

	public unsafe Vector3 Pos {
		get {
			var raw = Ptr->Position;
			return new Vector3(raw.X, raw.Z, raw.Y);
		}
		set {
			if (Removed) return;
			Ptr->Position = new Vector3(value.X, value.Z, value.Y);
		}
	}

	public float Angle {
		get => Angles.X;
		set => Angles = new Vector3(value, 0, 0);
	}

	public unsafe Vector3 Angles {
		get {
			var raw = Ptr->Rotation;
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
		set {
			if (Removed) return;
			var q = Quaternion.CreateFromYawPitchRoll(value.Z, value.Y, value.X); // θy, θx, θ
			Ptr->Rotation =new Quaternion(q.X, q.Z, q.Y, q.W) ;
		}
	}

	public unsafe Vector3 Scales {
		get {
			var raw = Ptr->Scale;
			return new Vector3(raw.X, raw.Z, raw.Y);
		}
		set {
			if (Removed) return;
			Ptr->Scale = new Vector3(value.X, value.Z, value.Y);
		}
	}

	public unsafe int ActorVfxSource {
		get => Ptr->ActorCaster;
		set {
			if (Removed) return;
			Ptr->ActorCaster = value;
		}
	}

	public unsafe int ActorVfxTarget {
		get => Ptr->ActorTarget;
		set {
			if (Removed) return;
			Ptr->ActorTarget = value;
		}
	}

	public unsafe int StaticVfxSource {
		get => Ptr->StaticCaster;
		set {
			if (Removed) return;
			Ptr->StaticCaster = value;
		}
	}

	public unsafe int StaticVfxTarget {
		get => Ptr->StaticTarget;
		set {
			if (Removed) return;
			Ptr->StaticTarget = value;
		}
	}

	public unsafe float Speed {
		get {
			SafeMemory.Read<float>((IntPtr)(Ptr + 0x250), out var res);
			return res;
		}
		set {
			if (Removed) return;
			SafeMemory.Write((IntPtr)(Ptr + 0x250), value);
		}
	}

	public unsafe Vector4 Color {
		get => Ptr->Color;
		set {
			if (Removed) return;
			Ptr->Color = value;
		}
	}
}