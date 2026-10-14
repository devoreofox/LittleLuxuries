using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using KamiToolKit.MapOverlay;
using LittleLuxuries.UI;
using LittleLuxuries.UI.PlayerMapIcons;

namespace LittleLuxuries.Tweaks;

public sealed unsafe class PlayerMapIcons : Tweak, IDisposable
{
    public override string Name => "Player Map Icons";
    public override string Description => "Shows nearby players on the map and, optionally, the minimap, filtered to the people you actually want to see.";

    private readonly IDataManager dataManager;
    private readonly IContextMenu contextMenu;
    private readonly Action togglePlayerListWindow;
    private readonly Configuration configuration;

    private readonly IClientState clientState;
    private readonly ITextureProvider textureProvider;

    private MapOverlayController? _mapOverlay;
    private MinimapMarkers? _minimap;

    private const float MinDistance = 10.0f;
    private const float MaxDistance = 500.0f;

    public PlayerMapIcons(IFramework framework, IDataManager dataManager, IContextMenu contextMenu, IClientState clientState, ITextureProvider textureProvider, Task ktkReady, Action togglePlayerListWindow, Configuration configuration)
    {
        this.dataManager = dataManager;
        this.contextMenu = contextMenu;
        this.clientState = clientState;
        this.textureProvider = textureProvider;
        this.togglePlayerListWindow = togglePlayerListWindow;
        this.configuration = configuration;

        contextMenu.OnMenuOpened += OnMenuOpened;
        ktkReady.ContinueWith(_ => framework.RunOnFrameworkThread(CreateOverlays), TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    public void Dispose()
    {
        contextMenu.OnMenuOpened -= OnMenuOpened;
        _minimap?.Dispose();
        _mapOverlay?.Dispose();
    }

    private void CreateOverlays()
    {
        _mapOverlay = new MapOverlayController();
        for (var i = 0; i < CharacterManager.Instance()->BattleCharas.Length; i++)
        {
            var marker = new PlayerMapMarker { PlayerIndex = i, Tweak = this };
            marker.AddLayers(textureProvider);
            _mapOverlay.AddMarker(marker);
        }
        _mapOverlay.Enable();

        _minimap = new MinimapMarkers(this, textureProvider, clientState, dataManager, configuration);
    }

    public Vector3? MarkerColor(BattleChara* chara, GameObject* local)
    {
        if (!configuration.PlayerMapIcons) return null;
        if ((GameObject*)chara == local) return null;
        if (chara->ObjectKind is not ObjectKind.Pc) return null;
        if (chara->IsPartyMember || chara->IsAllianceMember) return null;
        if (!chara->GetIsTargetable()) return null;
        if (Vector3.Distance(chara->Position, local->Position) > configuration.PlayerMapIconsDistance) return null;

        var key = $"{chara->NameString}@{ExcelWorldHelper.GetName(chara->HomeWorld)}";
        if (configuration.PlayerMapIconsBlacklist.Contains(key)) return null;
        if (configuration.PlayerMapIconsWhitelisted && configuration.PlayerMapIconsWhitelist.Contains(key)) return WhitelistColor(key);
        if (configuration.PlayerMapIconsFriends && chara->IsFriend) return configuration.PlayerMapIconsColors.Friends;
        if (configuration.PlayerMapIconsFreeCompany && SameFreeCompany(chara, (Character*)local)) return configuration.PlayerMapIconsColors.FreeCompany;
        if (configuration.PlayerMapIconsAll) return configuration.PlayerMapIconsColors.AllPlayers;
        return null;
    }

    private static bool SameFreeCompany(BattleChara* chara, Character* local)
    {
        var tag = local->FreeCompanyTagString;
        return tag.Length > 0 && tag == chara->FreeCompanyTagString;
    }

    public void AddToList(string key, bool whitelist)
    {
        var (add, remove) = whitelist
            ? (configuration.PlayerMapIconsWhitelist, configuration.PlayerMapIconsBlacklist)
            : (configuration.PlayerMapIconsBlacklist, configuration.PlayerMapIconsWhitelist);
        remove.Remove(key);
        add.Add(key);
        if (!whitelist) configuration.PlayerMapIconsPlayerColors.Remove(key);
        configuration.Save();
    }

    public void RemoveFromLists(string key)
    {
        configuration.PlayerMapIconsWhitelist.Remove(key);
        configuration.PlayerMapIconsBlacklist.Remove(key);
        configuration.PlayerMapIconsPlayerColors.Remove(key);
        configuration.Save();
    }

    public Vector3 WhitelistColor(string key) =>
        configuration.PlayerMapIconsPlayerColors.TryGetValue(key, out var color) ? color : configuration.PlayerMapIconsColors.Whitelisted;

    public bool HasCustomColor(string key) => configuration.PlayerMapIconsPlayerColors.ContainsKey(key);

    public void SetCustomColor(string key, Vector3 color) => configuration.PlayerMapIconsPlayerColors[key] = color;

    public void ClearCustomColor(string key)
    {
        if (configuration.PlayerMapIconsPlayerColors.Remove(key)) configuration.Save();
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (!configuration.PlayerMapIcons) return;
        if (args.Target is not MenuTargetDefault target || string.IsNullOrEmpty(target.TargetName)) return;
        if (target.TargetHomeWorld.RowId == 0) return;

        var key = $"{target.TargetName}@{target.TargetHomeWorld.Value.Name.ExtractText()}";

        if (configuration.PlayerMapIconsWhitelist.Contains(key) || configuration.PlayerMapIconsBlacklist.Contains(key))
        {
            args.AddMenuItem(Item("Reset Map Visibility", () => RemoveFromLists(key)));
            return;
        }

        args.AddMenuItem(Item("Always Show on Map", () => AddToList(key, true)));
        args.AddMenuItem(Item("Never Show on Map", () => AddToList(key, false)));
    }

    private static MenuItem Item(string name, Action onClicked) => new()
    {
        Name = name,
        Prefix = SeIconChar.BoxedLetterL,
        PrefixColor = 541,
        OnClicked = _ => onClicked()
    };

    public override void DrawConfig()
    {
        var enabled = configuration.PlayerMapIcons;
        if (ImGui.Checkbox("Show Players on Map", ref enabled))
        {
            configuration.PlayerMapIcons = enabled;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Shows nearby players on the map. Party members are already shown by the game.");

        if (!configuration.PlayerMapIcons) return;

        var colors = configuration.PlayerMapIconsColors;
        ImGui.Indent();
        var minimap = configuration.PlayerMapIconsMinimap;
        if (ImGui.Checkbox("Also Show on Minimap", ref minimap))
        {
            configuration.PlayerMapIconsMinimap = minimap;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Shows the same players on the minimap. Anyone past the edge of the minimap is pinned to its rim.");

        DistanceSetting();
        Option("All Players", configuration.PlayerMapIconsAll, v => configuration.PlayerMapIconsAll = v, ref colors.AllPlayers,
               "Show everyone nearby. Friends, FC members and whitelisted players keep their own colours.");
        Option("Friends", configuration.PlayerMapIconsFriends, v => configuration.PlayerMapIconsFriends = v, ref colors.Friends,
               "Show players on your friend list.");
        Option("Free Company Members", configuration.PlayerMapIconsFreeCompany, v => configuration.PlayerMapIconsFreeCompany = v, ref colors.FreeCompany,
               "Show players with your Free Company tag. Matches by tag, so another FC with the same tag will also show.");
        Option("Whitelisted Players", configuration.PlayerMapIconsWhitelisted, v => configuration.PlayerMapIconsWhitelisted = v, ref colors.Whitelisted,
               "Show players you've added to the whitelist. Turning this off keeps your list but stops them standing out.");
        ImGui.Unindent();

        ImGui.Spacing();
        if (ImGui.Button("Manage Players")) togglePlayerListWindow();
        ImGuiUtil.Tooltip("Whitelisted players always show, and can each be given their own colour. Blacklisted players never show, even with All Players on. You can also right-click a player to add them.");
    }

    private void Option(string label, bool value, Action<bool> set, ref Vector3 color, string tooltip)
    {
        ImGui.ColorEdit3($"##{label}Color", ref color, ImGuiColorEditFlags.NoInputs);
        if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();
        ImGuiUtil.Tooltip("Marker colour on the map.");
        ImGui.SameLine();

        if (ImGui.Checkbox(label, ref value))
        {
            set(value);
            configuration.Save();
        }
        ImGuiUtil.Tooltip(tooltip);
    }

    private void DistanceSetting()
    {
        var distance = configuration.PlayerMapIconsDistance;

        ImGui.SetNextItemWidth(150f);
        var changed = ImGui.SliderFloat("##playerMapIconsDistance", ref distance, MinDistance, MaxDistance, "%.0f");
        var finished = ImGui.IsItemDeactivatedAfterEdit();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(60f);
        changed |= ImGui.InputFloat("Max Distance##playerMapIconsDistanceInput", ref distance, 0f, 0f, "%.0f");
        finished |= ImGui.IsItemDeactivatedAfterEdit();

        if (changed) configuration.PlayerMapIconsDistance = Math.Clamp(distance, MinDistance, MaxDistance);
        if (finished) configuration.Save();
        ImGuiUtil.Tooltip($"How far away, in yalms, players can be and still show on the map ({MinDistance:0} to {MaxDistance:0}). Players outside the game's own loading range never show, whatever this is set to.");
    }
}
