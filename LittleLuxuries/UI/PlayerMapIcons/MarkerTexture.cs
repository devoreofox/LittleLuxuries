using System;
using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using KamiToolKit.Nodes;

namespace LittleLuxuries.UI.PlayerMapIcons;

public static class MarkerTexture
{
    private const int Size = 64;
    private const float Squareness = 0.3f;
    private const float OuterEdge = 0.9f;
    private const float BodyEdge = 0.72f;
    private const float CoreEdge = 0.5f;
    private const float RimShade = 0.45f;
    private const float BodyShade = 0.85f;
    private const float ShadowStrength = 0.35f;
    private const float HighlightEdge = 0.38f;
    private const float HighlightStrength = 0.9f;

    private static readonly byte[] BasePixels = Build(Base);
    private static readonly byte[] HighlightPixels = Build(Highlight);

    public static ImGuiImageNode CreateNode(ITextureProvider textureProvider, Vector2 size, bool highlight)
    {
        var node = new ImGuiImageNode { Size = size, FitTexture = true, IsVisible = true };
        node.LoadTexture(textureProvider.CreateFromRaw(RawImageSpecification.Rgba32(Size, Size), highlight ? HighlightPixels : BasePixels,
                                                       highlight ? "LittleLuxuries player marker highlight" : "LittleLuxuries player marker"));
        return node;
    }

    private static (float Color, float Alpha) Base(float d)
    {
        var shape = Math.Clamp((OuterEdge - d) * Size / 2.0f, 0.0f, 1.0f);
        var body = Math.Clamp((BodyEdge - d) * Size / 2.0f, 0.0f, 1.0f);
        var core = Math.Clamp((CoreEdge - d) / 0.4f, 0.0f, 1.0f);
        var shadow = d < 1.0f ? MathF.Min(ShadowStrength, (1.0f - d) / (1.0f - OuterEdge) * ShadowStrength) : 0.0f;

        var value = RimShade + (BodyShade - RimShade) * body + (1.0f - BodyShade) * core;
        var alpha = shape + (1.0f - shape) * shadow;
        return (alpha > 0.0f ? value * shape / alpha : 0.0f, alpha);
    }

    private static (float Color, float Alpha) Highlight(float d) =>
        (1.0f, MathF.Pow(Math.Clamp((HighlightEdge - d) / HighlightEdge, 0.0f, 1.0f), 1.5f) * HighlightStrength);

    private static byte[] Build(Func<float, (float Color, float Alpha)> shade)
    {
        var pixels = new byte[Size * Size * 4];
        var half = Size / 2.0f;

        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var dx = MathF.Abs(x + 0.5f - half) / half;
            var dy = MathF.Abs(y + 0.5f - half) / half;
            var (color, alpha) = shade(Squareness * (dx + dy) + (1.0f - Squareness) * MathF.Sqrt(dx * dx + dy * dy));

            var i = (y * Size + x) * 4;
            pixels[i] = pixels[i + 1] = pixels[i + 2] = (byte)(color * 255);
            pixels[i + 3] = (byte)(alpha * 255);
        }

        return pixels;
    }
}
