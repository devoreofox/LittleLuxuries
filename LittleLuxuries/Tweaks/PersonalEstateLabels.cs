using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LittleLuxuries.Models.Housing;
using LittleLuxuries.UI;
using Lumina.Excel.Sheets;

namespace LittleLuxuries.Tweaks;

public class PersonalEstateLabels : Tweak, IDisposable
{
    private readonly IDataManager dataManager;
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly Configuration configuration;
    private readonly IAddonLifecycle addonLifecycle;

    public PersonalEstateLabels(IDataManager dataManger,  IClientState clientState,  IPlayerState playerState,   Configuration configuration,  IAddonLifecycle addonLifecycle)
    {
        this.dataManager = dataManger;
        this.clientState = clientState;
        this.playerState = playerState;
        this.configuration = configuration;
        this.addonLifecycle = addonLifecycle;

        clientState.Login +=  OnLogin;

        if (clientState.IsLoggedIn) OnLogin();

        addonLifecycle.RegisterListener(AddonEvent.PreSetup, "Teleport", OnTeleportPreSetup);
    }
    public override string Name => "Personal Estate Labels";
    public override string Description => "Give your personal estate, shared estates and apartment custom names in the Teleport menu, optionally with ward and plot numbers, and reorder them so your favourite place sits at the top.";
    public override bool IsImplemented => true;

    public void Dispose()
    {
        clientState.Login -= OnLogin;
        addonLifecycle.UnregisterListener(OnTeleportPreSetup);
    }

    private unsafe void RefreshEstates()
    {
        if (!configuration.EstateLabels.TryGetValue(playerState.ContentId, out var character)) return;

        var changed = false;
        foreach (var t in Telepo.Instance()->TeleportList.AsSpan())
        {
            if (t.EstateType == (EstateType)255) continue;

            var placeName = dataManager.GetExcelSheet<TerritoryType>().GetRow(t.TerritoryId).PlaceName.Value.Name.ExtractText();
            var location = FormatLocation(t.HouseId, placeName);

            if (character.Estates.TryGetValue(t.HouseId.Id, out var existing))
            {
                if (existing.Location == location) continue;
                existing.Location = location;
            }
            else
            {
                character.Estates[t.HouseId.Id] = new EstateLabel { Location = location, Order = character.Estates.Count };
            }
            changed = true;
        }

        if (changed) configuration.Save();
    }

    private string FormatLocation(HouseId h, string placeName)
    {
        var ward = h.WardIndex + 1;
        if (configuration.EstateLabelsNumbers)
        {
            return h.IsApartment
                       ? $"{placeName} (W{ward}{(h.ApartmentDivision == 1 ? " Sub" : "")} Apt {h.RoomNumber})"
                       : $"{placeName} (W{ward} P{h.PlotIndex + 1})";
        }

        return placeName;
    }

    private void Move(List<KeyValuePair<ulong, EstateLabel>> estates, int j, int dir)
    {
        (estates[j], estates[j + dir]) = (estates[j + dir], estates[j]);
        for (var k = 0; k < estates.Count; k++) estates[k].Value.Order = k;
        configuration.Save();
    }

    private void OnLogin()
    {
        if (!configuration.PersonalEstateLabels) return;

        var charID = playerState.ContentId;
        if (charID == 0) return;
        if (!configuration.EstateLabels.TryGetValue(charID, out var character)) configuration.EstateLabels[charID] = character = new EstateLabelCharacter();
        character.Name = $"{playerState.CharacterName} @ {playerState.HomeWorld.Value.Name.ExtractText()}";
        configuration.Save();
    }

    private unsafe void OnTeleportPreSetup(AddonEvent type, AddonArgs args)
    {
        if (!configuration.PersonalEstateLabels) return;

        RefreshEstates();
        if (!configuration.EstateLabels.TryGetValue(playerState.ContentId, out var character)) return;

        var setup = (AddonSetupArgs)args;
        var values = (AtkValue*)setup.AtkValues;
        var teleports = Telepo.Instance()->TeleportList.AsSpan();
        var rows = new List<(int Start, int Order)>();

        for (var i = 2; i + 7 < setup.AtkValueCount; i += 8)
        {
            var idx = values[i + 3].Int;
            if (idx < 0)
            {
                if (i > 2) break;
                continue;
            }
            if (idx >= teleports.Length) break;
            if (!character.Estates.TryGetValue(teleports[idx].HouseId.Id, out var estate)) continue;

            rows.Add((i, estate.Order));
            if (string.IsNullOrWhiteSpace(estate.Label)) continue;

            var labelFirst = configuration.EstateLabelsLabelFirst;
            values[i + 4].SetManagedString(labelFirst ? estate.Label : estate.Location);
            values[i + 5].SetManagedString(labelFirst ? estate.Location : estate.Label);
        }

        var sorted = rows.OrderBy(r => r.Order).ToArray();
        var payloads = new AtkValue[rows.Count * 5];

        for (var r = 0; r < sorted.Length; r++)
        {
            for (var k = 0; k < 5; k++) payloads[r * 5 + k] = values[sorted[r].Start + 3 + k];
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var k = 0; k < 5; k++) values[rows[r].Start + 3 + k] = payloads[r * 5 + k];
        }
    }

    public override void DrawConfig()
    {
        var enabled = configuration.PersonalEstateLabels;
        var labelFirst = configuration.EstateLabelsLabelFirst;
        var labelNumbers = configuration.EstateLabelsNumbers;

        if (ImGui.Checkbox("Show custom estate labels in the Teleport menu", ref enabled))
        {
            configuration.PersonalEstateLabels = enabled;
            configuration.Save();

            if (enabled && clientState.IsLoggedIn) OnLogin();
        }

        if (ImGui.Checkbox("Show label before location", ref labelFirst))
        {
            configuration.EstateLabelsLabelFirst = labelFirst;
            configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Checkbox("Show ward and plot numbers", ref labelNumbers))
        {
            configuration.EstateLabelsNumbers = labelNumbers;
            configuration.Save();
            RefreshEstates();
        }

        foreach (var (cid, character) in configuration.EstateLabels)
        {
            if (!ImGui.CollapsingHeader($"{character.Name}##{cid}")) continue;
            if (character.Estates.Count == 0) ImGui.TextDisabled("Open the Teleport menu once to load your estates.");

            var estates = character.Estates.OrderBy(e => e.Value.Order).ToList();
            for (var j = 0; j < estates.Count; j++)
            {
                var (houseId, estate) = estates[j];

                ImGui.BeginDisabled(j == 0);
                if (ImGui.ArrowButton($"##up{houseId}", ImGuiDir.Up)) Move(estates, j, -1);
                ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.BeginDisabled(j == estates.Count - 1);
                if (ImGui.ArrowButton($"##down{houseId}", ImGuiDir.Down)) Move(estates, j, +1);
                ImGui.EndDisabled();
                ImGui.SameLine();

                var label = estate.Label;
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText($"{estate.Location}##{houseId}", ref label, 64)) estate.Label = label;
                if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();

                ImGui.SameLine();
                if (ImGui.SmallButton($"Forget##forget{houseId}"))
                {
                    character.Estates.Remove(houseId);
                    configuration.Save();
                }
                ImGuiUtil.Tooltip("Remove this estate and its label. Use it for places you've moved out of.");
            }
        }
    }
}
