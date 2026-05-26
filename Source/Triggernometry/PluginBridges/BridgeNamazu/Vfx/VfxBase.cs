using System;
using System.Numerics;
using Dalamud;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Triggernometry.PluginBridges.BridgeNamazu.Vfx;

public abstract class VfxBase {
	public unsafe VfxObject* Vfx { get; set; }
	public string Path { get; set; }
	public string Tag { get; set; }
	public bool Removed { get; set; }
	public DateTime? ExpireAtUtc { get; internal set; }

	public const string DefaultTag = "Auto";
	public  bool TryRemove()=>VfxManager.Remove(this);

	public void ScheduleRemove(double duration)
		=> VfxManager.ScheduleRemove(this, duration);

	public unsafe void Update() {
		if (Removed) return;
		Vfx->UpdateTransforms( true );
	}

	public unsafe byte Flag {
		get => (byte)Vfx->ObjectFlags;
		set {
			if (Removed) return;
			Vfx->ObjectFlags = value;
		}
	}

	public unsafe Vector3 Pos {
		get {
			var raw = Vfx->Position;
			return new Vector3(raw.X, raw.Z, raw.Y);
		}
		set {
			if (Removed) return;
			Vfx->Position = new Vector3(value.X, value.Z, value.Y);
		}
	}

	public float Angle {
		get => Angles.X;
		set => Angles = new Vector3(value, 0, 0);
	}

	public unsafe Vector3 Angles {
		get {
			var raw = Vfx->Rotation;
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
			Vfx->Rotation = new Quaternion(q.X, q.Z, q.Y, q.W);
		}
	}

	public unsafe Vector3 Scales {
		get {
			var raw = Vfx->Scale;
			return new Vector3(raw.X, raw.Z, raw.Y);
		}
		set {
			if (Removed) return;
			Vfx->Scale = new Vector3(value.X, value.Z, value.Y);
		}
	}

	public unsafe int ActorVfxSource {
		get => Vfx->ActorCaster;
		set {
			if (Removed) return;
			Vfx->ActorCaster = value;
		}
	}

	public unsafe int ActorVfxTarget {
		get => Vfx->ActorTarget;
		set {
			if (Removed) return;
			Vfx->ActorTarget = value;
		}
	}

	public unsafe int StaticVfxSource {
		get => Vfx->StaticCaster;
		set {
			if (Removed) return;
			Vfx->StaticCaster = value;
		}
	}

	public unsafe int StaticVfxTarget {
		get => Vfx->StaticTarget;
		set {
			if (Removed) return;
			Vfx->StaticTarget = value;
		}
	}

	public unsafe float Speed {
		get {
			SafeMemory.Read<float>((IntPtr)(Vfx + 0x250), out var res);
			return res;
		}
		set {
			if (Removed) return;
			SafeMemory.Write((IntPtr)(Vfx + 0x250), value);
		}
	}

	public unsafe Vector4 Color {
		get => Vfx->Color;
		set {
			if (Removed) return;
			Vfx->Color = value;
		}
	}
}