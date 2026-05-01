namespace FrankenTui.Demo.Showcase;

public static class ShowcaseVfxEffects
{
    public static readonly string[] AllCanonicalKeys =
    [
        "metaballs",
        "shape3d",
        "plasma",
        "particles",
        "matrix",
        "tunnel",
        "fire",
        "reaction-diffusion",
        "strange-attractor",
        "mandelbrot",
        "lissajous",
        "flow-field",
        "julia",
        "wave-interference",
        "spiral",
        "spin-lattice",
        "threejs-model",
        "doom-e1m1",
        "quake-e1m1"
    ];

    public static string? NormalizeName(string? effect)
    {
        var key = effect?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return key switch
        {
            "metaballs" => "metaballs",
            "shape3d" or "shape-3d" or "shape" => "shape3d",
            "plasma" => "plasma",
            "particles" or "fireworks" => "particles",
            "matrix" => "matrix",
            "tunnel" => "tunnel",
            "fire" => "fire",
            "reaction-diffusion" or "reaction_diffusion" or "rd" => "reaction-diffusion",
            "strange-attractor" or "strange_attractor" or "attractor" => "strange-attractor",
            "mandelbrot" or "mandel" => "mandelbrot",
            "lissajous" or "harmonograph" => "lissajous",
            "flow-field" or "flow_field" => "flow-field",
            "julia" => "julia",
            "wave" or "wave-interference" or "wave_interference" => "wave-interference",
            "spiral" or "galaxy" => "spiral",
            "spin-lattice" or "spin_lattice" => "spin-lattice",
            "threejs" or "threejs-model" or "three-model" or "model" or "model-3d" => "threejs-model",
            "doom" or "doom-e1m1" => "doom-e1m1",
            "quake" or "quake-e1m1" or "e1m1" => "quake-e1m1",
            _ => null
        };
    }

    public static string NormalizeOrDefault(string? effect) =>
        NormalizeName(effect) ?? "plasma";

    public static string DisplayName(string? effect) =>
        NormalizeOrDefault(effect) switch
        {
            "metaballs" => "Metaballs",
            "shape3d" => "3D Shape",
            "plasma" => "Plasma",
            "particles" => "Particles",
            "matrix" => "Matrix Rain",
            "tunnel" => "Tunnel",
            "fire" => "Fire",
            "reaction-diffusion" => "Reaction Diffusion",
            "strange-attractor" => "Strange Attractor",
            "mandelbrot" => "Mandelbrot",
            "lissajous" => "Lissajous",
            "flow-field" => "Flow Field",
            "julia" => "Julia",
            "wave-interference" => "Wave Interference",
            "spiral" => "Spiral",
            "spin-lattice" => "Spin Lattice",
            "threejs-model" => "Three.js Model",
            "doom-e1m1" => "Doom E1M1",
            "quake-e1m1" => "Quake E1M1",
            _ => "Plasma"
        };

    public static string Description(string? effect) =>
        NormalizeOrDefault(effect) switch
        {
            "metaballs" => "Organic blobs with implicit surface motion.",
            "shape3d" => "Rotating wireframe geometry with projected depth.",
            "plasma" => "Layered trigonometric plasma field.",
            "particles" => "Sparse moving particles and expanding rings.",
            "matrix" => "Columnar digital rain with deterministic drift.",
            "tunnel" => "Radial tunnel interference pattern.",
            "fire" => "Rising flame field with frame-driven turbulence.",
            "reaction-diffusion" => "Cellular reaction-diffusion texture.",
            "strange-attractor" => "Curved attractor traces from periodic fields.",
            "mandelbrot" => "Mandelbrot-style fractal boundary bands.",
            "lissajous" => "Lissajous wave crossings and harmonic sweeps.",
            "flow-field" => "Particle-like flow-field streaks.",
            "julia" => "Julia-style fractal boundary bands.",
            "wave-interference" => "Interference rings from multiple sources.",
            "spiral" => "Logarithmic spiral arm sweep.",
            "spin-lattice" => "Alternating lattice cells with spin noise.",
            "threejs-model" => "Local terminal analogue of a rotating model.",
            "doom-e1m1" => "FPS-style E1M1 grid and corridor raster.",
            "quake-e1m1" => "FPS-style Quake slipgate raster approximation.",
            _ => "Layered trigonometric plasma field."
        };

    public static bool IsFpsEffect(string? effect) =>
        NormalizeOrDefault(effect) is "doom-e1m1" or "quake-e1m1";

    public static string RendererName(string? effect) =>
        IsFpsEffect(effect) ? "local-fps-braille-canvas" : "local-effect-braille-canvas";

    public static string? NormalizeHarnessInput(string? effect)
    {
        var key = effect?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return NormalizeName(key) ?? key;
    }
}
