using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace LittleLuxuries;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public string? LastSeenVersion { get; set; }

    public bool NewTweaksInitialized { get; set; } = false;
    public HashSet<string> NewTweaks { get; set; } = new();

    public bool HideHousingArrows { get; set; } = false;
    public bool PreventInteraction { get; set; } = false;

    public bool DeterministicPosing { get; set; } = false;
    public bool CposeOneBasedIndex { get; set; } = true;
    public int  CposeDelayMs       { get; set; } = 150;

    public bool CopyContactNames { get; set; } = false;
    public bool CopyContactWithWorld { get; set; } = true;

    public bool EstateKey { get; set; } = false;

    public HashSet<string> FurnishingWhitelist { get; set; } = new();
    public Dictionary<ulong, Dictionary<uint, string>> UserWhitelist { get; set; } = new();

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
