using System;

namespace Triggernometry.UI.Aura.Renderer;

internal abstract class RendererState : IDisposable {
    public abstract void Dispose();
}