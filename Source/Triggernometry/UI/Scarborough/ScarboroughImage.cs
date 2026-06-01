using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Triggernometry.Core;
using Color = Scarborough.Drawing.Color;
using Graphics = Scarborough.Drawing.Graphics;
using Image = Scarborough.Drawing.Image;
using Rectangle = Scarborough.Drawing.Rectangle;

namespace Scarborough;

internal class ScarboroughImage : ScarboroughItem {
	private int CurrentFrame { get; set; }
	private int CurrentFrameDelay { get; set; }
	private int NumberOfFrames = 1;
	private bool IsAnimated;
	public List<int> FrameDelays { get; set; }
	public List<Image> Frames { get; set; }

	internal bool NeedImage { get; set; }
	internal string ImageFilename { get; set; }
	internal string ImageExpression { get; set; }

	private PictureBoxSizeMode _Display;
	internal PictureBoxSizeMode Display {
		get => _Display;
		set {
			if (value != _Display) {
				Changed = true;
				_Display = value;
			}
		}
	}

	private Image OriginalImage { get; set; }

	private byte[] ImageToByte(System.Drawing.Image img) {
		var converter = new ImageConverter();
		return (byte[])converter.ConvertTo(img, typeof(byte[]));
	}

	internal sealed class GifData {
		public int TransparencyIndex { get; set; } = -1;
		public int BackgroundColor { get; set; } = -1;
		public bool HasGCTF { get; set; }
		public int GCTFSize { get; set; } = -1;
		public int GCTFColors { get; set; } = -1;
		public Color[] Palette { get; set; }
	}

	internal GifData GetGifData(byte[] data) {
		if (data[0] == 71 || data[1] == 73 || data[2] == 70) {
			var g = new GifData();
			g.BackgroundColor = data[11];
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

	private double TimeAccumulator;
	private DateTime LastAdvance = DateTime.MinValue;

	public ScarboroughImage(Triggernometry.UI.Scarborough own) : base(own) {
	}

	public bool AdvanceFrame() {
		var prev = CurrentFrame;
		if (CurrentFrame == -1) {
			LastAdvance = DateTime.Now;
			TimeAccumulator = 0.0;
			CurrentFrame = 0;
			CurrentFrameDelay = FrameDelays[0];
			OriginalImage = Frames[CurrentFrame];
		} else {
			TimeAccumulator += (DateTime.Now - LastAdvance).TotalMilliseconds;
			LastAdvance = DateTime.Now;
			while (TimeAccumulator >= CurrentFrameDelay) {
				CurrentFrame++;
				if (CurrentFrame >= NumberOfFrames) {
					CurrentFrame = 0;
				}
				TimeAccumulator -= CurrentFrameDelay;
				CurrentFrameDelay = FrameDelays[CurrentFrame];
				if (CurrentFrameDelay <= 0) {
					break;
				}
			}
			OriginalImage = Frames[CurrentFrame];
		}
		return prev != CurrentFrame;
	}

	public void LoadImageOnDemand() {
		Free();
		LoadImageData(plug, _graphics, ImageFilename);
	}

	public override void Free() {
		if (NumberOfFrames > 1) {
			foreach (var i in Frames) {
				i.Dispose();
			}
			Frames.Clear();
			OriginalImage = null;
		} else {
			if (OriginalImage != null) {
				OriginalImage.Dispose();
				OriginalImage = null;
			}
		}
	}

	public override void Render() {
		if (NeedImage) {
			if (_window == null) {
				if (!AdjustSurface()) {
					AdjustVisibility();
					return;
				}
			}
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
		if (!AdjustSurface()) {
			AdjustVisibility();
			return;
		}
		AdjustVisibility();
		NeedRender = false;
		_graphics.BeginScene();
		_graphics.ClearScene(_bgColor);
		if (!Owner.RenderingActive || InvalidSize) {
			_graphics.EndScene();
			return;
		}
		switch (Display) {
			case PictureBoxSizeMode.Zoom:
				var mW = Width / OriginalImage.Width;
				var mH = Height / OriginalImage.Height;
				float aW, aH;
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
				_graphics.DrawImage(
					OriginalImage,
					(Width - aW) / 2,
					(Height - aH) / 2,
					(Width - aW) / 2 + aW,
					(Height - aH) / 2 + aH,
					Opacity / 100.0f
				);
				break;
			case PictureBoxSizeMode.Normal:
				if (OriginalImage.Width <= Width && OriginalImage.Height <= Height) {
					_graphics.DrawImage(
						OriginalImage,
						0,
						0,
						OriginalImage.Width,
						OriginalImage.Height,
						Opacity / 100.0f
					);
				} else {
					if (OriginalImage.Width <= Width) {
						_graphics.DrawImageEx(
							OriginalImage,
							new Rectangle(0, 0, OriginalImage.Width, Height),
							new Rectangle(0, 0, OriginalImage.Width, Height),
							Opacity / 100.0f
						);
					} else if (OriginalImage.Height <= Height) {
						_graphics.DrawImageEx(
							OriginalImage,
							new Rectangle(0, 0, Width, OriginalImage.Height),
							new Rectangle(0, 0, Width, OriginalImage.Height),
							Opacity / 100.0f
						);
					} else {
						_graphics.DrawImageEx(
							OriginalImage,
							new Rectangle(0, 0, Width, Height),
							new Rectangle(0, 0, Width, Height),
							Opacity / 100.0f
						);
					}
				}
				break;
			case PictureBoxSizeMode.CenterImage:
				if (OriginalImage.Width <= Width && OriginalImage.Height <= Height) {
					_graphics.DrawImage(
						OriginalImage,
						(Width - OriginalImage.Width) / 2,
						(Height - OriginalImage.Height) / 2,
						(Width - OriginalImage.Width) / 2 + OriginalImage.Width,
						(Height - OriginalImage.Height) / 2 + OriginalImage.Height,
						Opacity / 100.0f
					);
				} else {
					_graphics.DrawImageEx(
						OriginalImage,
						new Rectangle(
							OriginalImage.Width / 2 - Width / 2,
							OriginalImage.Height / 2 - Height / 2,
							OriginalImage.Width / 2 + Width / 2,
							OriginalImage.Height / 2 + Height / 2
						),
						new Rectangle(
							0,
							0,
							Width,
							Height
						),
						Opacity / 100.0f
					);
				}
				break;
			case PictureBoxSizeMode.StretchImage:
				_graphics.DrawImage(
					OriginalImage,
					0,
					0,
					Width,
					Height,
					Opacity / 100.0f
				);
				break;
		}
		_graphics.EndScene();
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
}