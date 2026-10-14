using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Numerics;
using LittleLuxuries.Models.Housing;
using LittleLuxuries.Models.PlayerMapIcons;

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
    public int  CposeDelayMs { get; set; } = 150;

    public bool CopyContactNames { get; set; } = false;
    public bool CopyContactWithWorld { get; set; } = true;

    public bool EstateKey { get; set; } = false;

    public bool CommendQueue { get; set; } = false;

    public bool QuickCommands { get; set; } = false;

    public bool PersonalEstateLabels { get; set; } = false;
    public Dictionary<ulong, EstateLabelCharacter> EstateLabels { get; set; } = new();
    public bool EstateLabelsNumbers { get; set; } = false;
    public bool EstateLabelsLabelFirst { get; set; } = false;
    public int EstateBookmarkTravelCity { get; set; } = 8;

    public bool PlayerMapIcons { get; set; } = false;
    public bool PlayerMapIconsMinimap { get; set; } = false;
    public bool PlayerMapIconsAll { get; set; } = false;
    public bool PlayerMapIconsFriends { get; set; } = true;
    public bool PlayerMapIconsFreeCompany { get; set; } = false;
    public bool PlayerMapIconsWhitelisted { get; set; } = true;
    public float PlayerMapIconsDistance { get; set; } = 150.0f;
    public HashSet<string> PlayerMapIconsWhitelist { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> PlayerMapIconsBlacklist { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public MarkerColors PlayerMapIconsColors { get; set; } = new();
    public Dictionary<string, Vector3> PlayerMapIconsPlayerColors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> FurnishingWhitelist { get; set; } = new();
    public Dictionary<ulong, Dictionary<uint, string>> UserWhitelist { get; set; } = new();

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
