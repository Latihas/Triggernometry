using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Triggernometry.Core;
using Triggernometry.UI.Aura;
using TriggernometryProxy;
using Color = Scarborough.Drawing.Color;
using Graphics = Scarborough.Drawing.Graphics;
using Image = Scarborough.Drawing.Image;
using GdiGraphics = System.Drawing.Graphics;
using DrawingRectangle = Scarborough.Drawing.Rectangle;

namespace Scarborough;

public class ScarboroughImage(Triggernometry.UI.Scarborough own) : ScarboroughItem(own) {
	private bool IsAnimated;
	private DateTime LastAdvance = DateTime.MinValue;
	private int NumberOfFrames = 1;

	private double TimeAccumulator;

	private int CurrentFrame { get; set; }
	private int CurrentFrameDelay { get; set; }
	public List<int> FrameDelays { get; set; }
	public List<Image> Frames { get; set; }

	internal bool NeedImage { get; set; }
	internal string ImageFilename { get; set; }
	internal string ImageExpression { get; set; }
	internal PictureBoxSizeMode Display {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	private Image OriginalImage { get; set; }
	private List<IDalamudTextureWrap>? ImGuiFrames { get; set; }
	private IDalamudTextureWrap? CurrentImGuiFrame { get; set; }
	private ISharedImmediateTexture? ImGuiTexture { get; set; }

	private byte[] ImageToByte(System.Drawing.Image img) {
		var converter = new ImageConverter();
		return (byte[])converter.ConvertTo(img, typeof(byte[]));
	}

	internal GifData GetGifData(byte[] data) {
		if (data[0] == 71 || data[1] == 73 || data[2] == 70) {
			var g = new GifData {
				BackgroundColor = data[11]
			};
			var ii = 10;
			g.HasGCTF = (data[ii] & 0x80) > 0;
			if (g.HasGCTF) {
				// if we have a GCTF
				g.GCTFColors = (int)Math.Pow(2, (data[ii] & 0x07) + 1);
				g.GCTFSize = 3 * g.GCTFColors;
				g.Palette = new Color[g.GCTFColors];
				ii += 3;
				for (var c = 0; c < g.GCTFColors; c++) {
					g.Palette[c] = new Color(data[ii], data[ii + 1], data[ii + 2]);
					ii += 3;
				}
			}
			while (ii < data.Length) {
				if (data[ii] == 0x21) {
					if (data[ii + 1] == 0xf9) {
						g.TransparencyIndex = data[ii + 6];
						break;
					}
					int bsize = data[ii + 2];
					ii += bsize + 3;
					while (data[ii] != 0) {
						ii += data[ii] + 1;
					}
					ii++;
				} else {
					// out of blocks
					break;
				}
			}
			return g;
		}
		return null;
	}

	internal void SetTransparencyIndex(byte[] data, byte idx) {
		if (data[0] == 71 || data[1] == 73 || data[2] == 70) {
			var ii = 10;
			if ((data[ii] & 0x80) > 0) {
				// if we have a GCTF
				var gctfsize = 3 * (int)Math.Pow(2, (data[ii] & 0x07) + 1);
				ii += gctfsize;
			}
			ii += 3;
			// should have application block here now
			if (data[ii] == 0x21 && data[ii + 1] == 0xff) {
				ii += 19;
				if (data[ii] == 0x21 && data[ii + 1] == 0xf9) {
					data[ii + 6] = idx;
				}
			}
		}
	}

	internal void LoadImageDataFromFile(RealPlugin plug, Graphics g, string fn) {
		var data = File.ReadAllBytes(fn);
		LoadImageDataFromByte(plug, g, data);
	}

	internal void LoadImageDataFromByte(RealPlugin plug, Graphics g, byte[] data) {
		var gif = GetGifData(data);
		using var ms = new MemoryStream(data);
		using var i = System.Drawing.Image.FromStream(ms);
		var b = (Bitmap)i;
		var CurrentFd = new FrameDimension(i.FrameDimensionsList[0]);
		NumberOfFrames = i.GetFrameCount(CurrentFd);
		IsAnimated = NumberOfFrames > 1;
		if (IsAnimated) {
			Frames = [];
			FrameDelays = [];
			var delay = i.GetPropertyItem(0x5100);
			Color tc;
			var hastc = false;
			if (gif.TransparencyIndex >= 0) {
				tc = gif.Palette[gif.TransparencyIndex];
				hastc = true;
			} else {
				tc = gif.Palette[0];
			}
			for (var h = 0; h < NumberOfFrames; h++) {
				var delayn = (delay.Value[h * 4] + delay.Value[h * 4 + 1] * 256) * 10;
				FrameDelays.Add(delayn);
				i.SelectActiveFrame(CurrentFd, h);
				var ifa = (System.Drawing.Image)i.Clone();
				var idata = ImageToByte(ifa);
				if (hastc) {
					if (gif.TransparencyIndex != gif.BackgroundColor) {
						// hack in case transparency color is different from bgcolor (some gifs have this shit)
						SetTransparencyIndex(idata, (byte)gif.BackgroundColor);
					} else {
						for (var j = 0; j < gif.TransparencyIndex; j++) {
							if (gif.Palette[j].R == tc.R && gif.Palette[j].R == tc.G && gif.Palette[j].R == tc.B) {
								// net itself might have selected an earlier color as new transparency color
								// hack to reset transparency index to match if so
								SetTransparencyIndex(idata, (byte)j);
								break;
							}
						}
					}
				}
				Frames.Add(g.CreateImage(idata));
			}
			CurrentFrame = -1;
			AdvanceFrame();
		} else {
			OriginalImage = g.CreateImage(data);
		}
	}

	internal void LoadImageData(RealPlugin plug, Graphics g, string ifn) {
		var u = new Uri(ifn);
		if (u.IsFile) {
			LoadImageDataFromFile(plug, g, ifn);
		} else {
			var fn = Path.Combine(plug.ConfigPath, "TriggernometryRemoteImages");
			if (!Directory.Exists(fn)) {
				Directory.CreateDirectory(fn);
			}
			var ext = Path.GetExtension(u.LocalPath);
			fn = Path.Combine(fn, RealPlugin.GenerateHash(u.AbsoluteUri) + Path.GetExtension(u.LocalPath));
			var fromcache = false;
			if (File.Exists(fn)) {
				var fi = new FileInfo(fn);
				var dt = DateTime.Now.AddMinutes(0 - plug.cfg.CacheImageExpiry);
				if (fi.LastWriteTime > dt) {
					LoadImageDataFromFile(plug, g, fn);
					fromcache = true;
				}
			}
			if (!fromcache) {
				using var wc = new WebClient();
				wc.Headers["User-Agent"] = "Triggernometry Image Retriever";
				var data = wc.DownloadData(u.AbsoluteUri);
				File.WriteAllBytes(fn, data);
				LoadImageDataFromByte(plug, g, data);
			}
		}
	}

	public bool AdvanceFrame() {
		var prev = CurrentFrame;
		if (CurrentFrame == -1) {
			LastAdvance = DateTime.Now;
			TimeAccumulator = 0.0;
			CurrentFrame = 0;
			CurrentFrameDelay = FrameDelays[0];
			SetCurrentFrame();
		} else {
			TimeAccumulator += (DateTime.Now - LastAdvance).TotalMilliseconds;
			LastAdvance = DateTime.Now;
			if (CurrentFrameDelay <= 0) CurrentFrameDelay = 1;
			while (TimeAccumulator >= CurrentFrameDelay) {
				CurrentFrame++;
				if (CurrentFrame >= NumberOfFrames) {
					CurrentFrame = 0;
				}
				TimeAccumulator -= CurrentFrameDelay;
				CurrentFrameDelay = FrameDelays[CurrentFrame];
			}
			SetCurrentFrame();
		}
		return prev != CurrentFrame;
	}

	public void LoadImageOnDemand() {
		Free();
		if (!Owner.IsImGuiRenderer) {
			LoadImageData(plug, _graphics!, ImageFilename);
			return;
		}

		var path = ResolveImagePath(ImageFilename);
		if (string.IsNullOrWhiteSpace(path) || ProxyPlugin.TextureProvider == null) return;

		try {
			if (LoadAnimatedImage(path)) return;
		} catch {
			Free();
		}

		ImGuiTexture = ProxyPlugin.TextureProvider.GetFromFileAbsolute(path);
	}

	public override void Free() {
		if (ImGuiFrames != null) {
			foreach (var frame in ImGuiFrames) frame.Dispose();
			ImGuiFrames.Clear();
			ImGuiFrames = null;
		}
		CurrentImGuiFrame = null;
		ImGuiTexture = null;

		if (Frames != null) {
			foreach (var i in Frames) {
				if (i != null && !ReferenceEquals(i, OriginalImage)) i.Dispose();
			}
			Frames.Clear();
		}
		OriginalImage?.Dispose();
		OriginalImage = null;
		IsAnimated = false;
		NumberOfFrames = 1;
		CurrentFrame = 0;
		CurrentFrameDelay = 0;
	}

	public override void Render() {
		if (NeedImage) {
			if (!AdjustSurface()) return;
			LoadImageOnDemand();
			NeedImage = false;
			NeedRender = true;
		}
		if (IsAnimated) {
			var af = AdvanceFrame();
			NeedRender = NeedRender || af;
		}
		if (!Owner.RenderingActive) {
			if (!WasHidden) {
				NeedRender = true;
				WasHidden = true;
			}
		} else {
			if (WasHidden) {
				NeedRender = true;
				WasHidden = false;
			}
		}
		if (!NeedRender) {
			return;
		}
		if (!AdjustSurface()) return;
		if (Owner.IsImGuiRenderer) {
			NeedRender = false;
			return;
		}

		AdjustVisibility();
		NeedRender = false;
		var graphics = _graphics!;
		graphics.BeginScene();
		graphics.ClearScene(_bgColor);
		if (!Owner.RenderingActive || InvalidSize || OriginalImage == null) {
			graphics.EndScene();
			return;
		}

		switch (Display) {
			case PictureBoxSizeMode.Zoom: {
				var mW = Width / OriginalImage.Width;
				var mH = Height / OriginalImage.Height;
				float aW;
				float aH;
				if (mH < mW) {
					aW = mH * OriginalImage.Width;
					aH = Height;
				} else if (mW < mH) {
					aW = Width;
					aH = mW * OriginalImage.Height;
				} else {
					aW = Width;
					aH = Height;
				}
				graphics.DrawImage(
					OriginalImage,
					(Width - aW) / 2,
					(Height - aH) / 2,
					(Width - aW) / 2 + aW,
					(Height - aH) / 2 + aH,
					Opacity / 100.0f);
				break;
			}
			case PictureBoxSizeMode.Normal:
				if (OriginalImage.Width <= Width && OriginalImage.Height <= Height) {
					graphics.DrawImage(
						OriginalImage,
						0,
						0,
						OriginalImage.Width,
						OriginalImage.Height,
						Opacity / 100.0f);
				} else if (OriginalImage.Width <= Width) {
					graphics.DrawImageEx(
						OriginalImage,
						new DrawingRectangle(0, 0, OriginalImage.Width, Height),
						new DrawingRectangle(0, 0, OriginalImage.Width, Height),
						Opacity / 100.0f);
				} else if (OriginalImage.Height <= Height) {
					graphics.DrawImageEx(
						OriginalImage,
						new DrawingRectangle(0, 0, Width, OriginalImage.Height),
						new DrawingRectangle(0, 0, Width, OriginalImage.Height),
						Opacity / 100.0f);
				} else {
					graphics.DrawImageEx(
						OriginalImage,
						new DrawingRectangle(0, 0, Width, Height),
						new DrawingRectangle(0, 0, Width, Height),
						Opacity / 100.0f);
				}
				break;
			case PictureBoxSizeMode.CenterImage:
				if (OriginalImage.Width <= Width && OriginalImage.Height <= Height) {
					graphics.DrawImage(
						OriginalImage,
						(Width - OriginalImage.Width) / 2,
						(Height - OriginalImage.Height) / 2,
						(Width - OriginalImage.Width) / 2 + OriginalImage.Width,
						(Height - OriginalImage.Height) / 2 + OriginalImage.Height,
						Opacity / 100.0f);
				} else {
					graphics.DrawImageEx(
						OriginalImage,
						new DrawingRectangle(
							OriginalImage.Width / 2 - Width / 2,
							OriginalImage.Height / 2 - Height / 2,
							OriginalImage.Width / 2 + Width / 2,
							OriginalImage.Height / 2 + Height / 2),
						new DrawingRectangle(0, 0, Width, Height),
						Opacity / 100.0f);
				}
				break;
			case PictureBoxSizeMode.StretchImage:
				graphics.DrawImage(
					OriginalImage,
					0,
					0,
					Width,
					Height,
					Opacity / 100.0f);
				break;
		}
		graphics.EndScene();
	}

	internal override void DrawImGui(ImDrawListPtr drawList) {
		if (!Owner.RenderingActive || InvalidSize) return;

		var texture = CurrentImGuiFrame;
		if (texture == null && ImGuiTexture != null) {
			try {
				texture = ImGuiTexture.GetWrapOrEmpty();
			} catch {
				return;
			}
		}
		if (texture == null) return;

		var imageWidth = texture.Width;
		var imageHeight = texture.Height;
		if (imageWidth <= 0 || imageHeight <= 0) return;

		var destination = new Vector2(Left, Top);
		var destinationSize = new Vector2(Width, Height);
		var sourceMin = Vector2.Zero;
		var sourceMax = Vector2.One;
		switch (Display) {
			case PictureBoxSizeMode.Zoom:
				var scale = MathF.Min(Width / (float)imageWidth, Height / (float)imageHeight);
				var scaledSize = new Vector2(imageWidth * scale, imageHeight * scale);
				destination += (destinationSize - scaledSize) / 2;
				destinationSize = scaledSize;
				break;
			case PictureBoxSizeMode.Normal:
				destinationSize = new Vector2(MathF.Min(Width, imageWidth), MathF.Min(Height, imageHeight));
				sourceMax = new Vector2(destinationSize.X / imageWidth, destinationSize.Y / imageHeight);
				break;
			case PictureBoxSizeMode.CenterImage:
				var centeredImagePosition = destination + (destinationSize - new Vector2(imageWidth, imageHeight)) / 2;
				var visibleMin = Vector2.Max(destination, centeredImagePosition);
				var visibleMax = Vector2.Min(
					destination + destinationSize,
					centeredImagePosition + new Vector2(imageWidth, imageHeight));
				if (visibleMax.X <= visibleMin.X || visibleMax.Y <= visibleMin.Y) return;
				sourceMin = new Vector2(
					(visibleMin.X - centeredImagePosition.X) / imageWidth,
					(visibleMin.Y - centeredImagePosition.Y) / imageHeight);
				sourceMax = new Vector2(
					(visibleMax.X - centeredImagePosition.X) / imageWidth,
					(visibleMax.Y - centeredImagePosition.Y) / imageHeight);
				destination = visibleMin;
				destinationSize = visibleMax - visibleMin;
				break;
			case PictureBoxSizeMode.StretchImage:
				break;
		}

		drawList.AddImage(
			texture.Handle,
			destination,
			destination + destinationSize,
			sourceMin,
			sourceMax,
			ToImGuiColor(System.Drawing.Color.White, Math.Clamp(Opacity, 0, 100) / 100.0f));
	}

	private void SetCurrentFrame() {
		if (ImGuiFrames != null && CurrentFrame >= 0 && CurrentFrame < ImGuiFrames.Count) {
			CurrentImGuiFrame = ImGuiFrames[CurrentFrame];
		}
		if (Frames != null && CurrentFrame >= 0 && CurrentFrame < Frames.Count) {
			OriginalImage = Frames[CurrentFrame];
		}
	}

	private bool LoadAnimatedImage(string path) {
		if (ProxyPlugin.TextureProvider == null) return false;

		using var stream = File.OpenRead(path);
		using var image = System.Drawing.Image.FromStream(stream);
		var dimensions = image.FrameDimensionsList;
		if (dimensions == null || dimensions.Length == 0) return false;

		var frameDimension = new FrameDimension(dimensions[0]);
		var frameCount = image.GetFrameCount(frameDimension);
		if (frameCount <= 1) return false;

		var delays = ReadFrameDelays(image, frameCount);
		var frames = new List<IDalamudTextureWrap>(frameCount);
		try {
			for (var frameIndex = 0; frameIndex < frameCount; frameIndex++) {
				image.SelectActiveFrame(frameDimension, frameIndex);
				using var frame = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
				using (var graphics = GdiGraphics.FromImage(frame)) {
					graphics.Clear(System.Drawing.Color.Transparent);
					graphics.DrawImageUnscaled(image, 0, 0);
				}

				var rgba = BitmapToRgba(frame);
				frames.Add(ProxyPlugin.TextureProvider.CreateFromRaw(
					RawImageSpecification.Rgba32(frame.Width, frame.Height),
					rgba,
					"Triggernometry Scarborough animated image"));
			}
		} catch {
			foreach (var frame in frames) frame.Dispose();
			throw;
		}

		if (frames.Count == 0) return false;

		ImGuiFrames = frames;
		FrameDelays = delays;
		NumberOfFrames = frames.Count;
		IsAnimated = true;
		CurrentFrame = -1;
		AdvanceFrame();
		return true;
	}

	private static List<int> ReadFrameDelays(System.Drawing.Image image, int frameCount) {
		var delays = new List<int>(frameCount);
		try {
			var property = image.GetPropertyItem(0x5100);
			var value = property.Value ?? [];
			for (var i = 0; i < frameCount; i++) {
				var offset = i * 4;
				var delay = offset + 1 < value.Length
					? value[offset] | value[offset + 1] << 8
					: 0;
				delays.Add(delay * 10);
			}
		} catch {
			for (var i = 0; i < frameCount; i++) delays.Add(100);
		}

		for (var i = 0; i < delays.Count; i++) {
			if (delays[i] <= 0) delays[i] = 100;
		}
		return delays;
	}

	private static byte[] BitmapToRgba(Bitmap bitmap) {
		var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
		var bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try {
			var rgba = new byte[bitmap.Width * bitmap.Height * 4];
			var sourceRow = new byte[bitmap.Width * 4];
			for (var y = 0; y < bitmap.Height; y++) {
				var sourceY = bitmapData.Stride < 0 ? bitmap.Height - 1 - y : y;
				Marshal.Copy(
					IntPtr.Add(bitmapData.Scan0, sourceY * bitmapData.Stride),
					sourceRow,
					0,
					sourceRow.Length);
				for (var x = 0; x < bitmap.Width; x++) {
					var source = x * 4;
					var target = (y * bitmap.Width + x) * 4;
					rgba[target] = sourceRow[source + 2];
					rgba[target + 1] = sourceRow[source + 1];
					rgba[target + 2] = sourceRow[source];
					rgba[target + 3] = sourceRow[source + 3];
				}
			}
			return rgba;
		} finally {
			bitmap.UnlockBits(bitmapData);
		}
	}

	private string? ResolveImagePath(string imageFilename) {
		if (string.IsNullOrWhiteSpace(imageFilename)) return null;
		if (!Uri.TryCreate(imageFilename, UriKind.Absolute, out var uri)) return Path.GetFullPath(imageFilename);
		if (uri.IsFile) return uri.LocalPath;
		return AuraImage.GetImageFilename(plug, imageFilename);
	}

	public override bool InternalLogic(int numTicks) {
		while (numTicks > 0) {
			if (!GenericLogic()) {
				return false;
			}
			numTicks--;
		}
		return true;
	}

	internal sealed class GifData {
		public int TransparencyIndex { get; set; } = -1;
		public int BackgroundColor { get; set; } = -1;
		public bool HasGCTF { get; set; }
		public int GCTFSize { get; set; } = -1;
		public int GCTFColors { get; set; } = -1;
		public Color[] Palette { get; set; }
	}
}
