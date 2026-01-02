using System;

namespace Triggernometry.UI.Aura.Renderer;

internal abstract class RendererBase : IDisposable {
    internal Manager Owner { get; set; }

    public abstract void Dispose();

    internal abstract void Initialize(Aura a);
    internal abstract void Render(Aura a);
}