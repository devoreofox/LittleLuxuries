using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ECommons.ExcelServices;
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

    private static readonly Dictionary<int, ushort> CityDistricts = new() { [8] = 339, [2] = 340, [9] = 341, [111] = 641, [70] = 979 };

    private string newLabel = "", worldFilter = "";
    private uint newWorld;
    private (string Region, (string DataCenter, (uint Id, string Name)[] Worlds)[] DataCenters)[]? worldTree;
    private string? addError;
    private int newCity, newWard = 1, newPlot = 1, newRoom = 1;
    private bool newIsApartment, newSub;

    private readonly Dictionary<uint, EstateBookmark> bookmarkBlocks = new();

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
        addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Teleport", OnTeleportReceiveEvent);
    }
    public override string Name => "Personal Estate Labels";
    public override string Description => "Give your personal estate, shared estates and apartment custom names in the Teleport menu, optionally with ward and plot numbers, and reorder them so your favourite place sits at the top.";
    public override bool IsImplemented => true;

    public void Dispose()
    {
        clientState.Login -= OnLogin;
        addonLifecycle.UnregisterListener(OnTeleportPreSetup, OnTeleportReceiveEvent);
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

    private string FormatBookmark(EstateBookmark b)
    {
        var world = dataManager.GetExcelSheet<World>().GetRow((uint)b.World).Name.ExtractText();
        var place = dataManager.GetExcelSheet<TerritoryType>().GetRow(CityDistricts[b.City]).PlaceName.Value.Name.ExtractText();
        var unit = b.IsApartment ? $"{(b.ApartmentSubdivision ? "Sub " : "")}Apt {b.Apartment}" : $"P{b.Plot}";
        return $"{world}, {place} (W{b.Ward} {unit})";
    }

    private void AddBookmark(EstateLabelCharacter character, int city)
    {
        if (string.IsNullOrWhiteSpace(newLabel))
        {
            addError = "Give the bookmark a label.";
            return;
        }

        if (newWorld == 0)
        {
            addError = "Pick a world.";
            return;
        }

        character.Bookmarks.Add(new EstateBookmark
        {
            Label = newLabel, World = (int)newWorld, City = city, Ward = newWard,
            IsApartment = newIsApartment, Plot = newPlot, Apartment = newRoom, ApartmentSubdivision = newSub,
        });
        configuration.Save();
        (newLabel, addError) = ("", null);
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
        bookmarkBlocks.Clear();
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

        if (rows.Count > 0)
        {
            var insertAt = rows.Max(r => r.Start) + 8;
            var rowKind = values[rows[0].Start + 1].UInt;
            var labelFirst = configuration.EstateLabelsLabelFirst;

            foreach (var b in character.Bookmarks)
            {
                var location = FormatBookmark(b);
                if (!InsertRow(values, setup.AtkValueCount, insertAt, rowKind, labelFirst ? b.Label : location, labelFirst ? location : b.Label)) break;
                bookmarkBlocks[(uint)((insertAt - 2) / 8)] = b;
                insertAt += 8;
            }
        }
    }

    private unsafe void OnTeleportReceiveEvent(AddonEvent type, AddonArgs args)
    {
        var e = (AddonReceiveEventArgs)args;
        if (e.AtkEventType != AddonEventType.ListItemClick) return;

        var data = (AtkEventData*)e.AtkEventData;
        if (data == null || data->ListItemData.ListItem == null || data->ListItemData.MouseButtonId != 0) return;

        var block = data->ListItemData.ListItem->UIntValues.AsSpan()[2];
        if (!bookmarkBlocks.TryGetValue(block, out var bookmark)) return;

        Serilog.Log.Information($"[EstateLabels] bookmark clicked: {bookmark.Label} -> {FormatBookmark(bookmark)}");
        // step 3: Lifestream or teleport fallback
    }

    private static unsafe bool InsertRow(AtkValue* values, uint valueCount, int insertAt, uint rowKind, string col1, string col2)
    {
        var count = (int)values[2].UInt;
        var end = 2 + count * 8;
        if (end + 8 >= valueCount || values[end + 8].Type != AtkValueType.Undefined) return false;

        var bytes = (end - insertAt + 1) * sizeof(AtkValue);
        Buffer.MemoryCopy(values + insertAt, values + insertAt + 8, bytes, bytes);

        for (var k = 0; k < 8; k++) values[insertAt + k] = default;

        values[insertAt + 0].Type = AtkValueType.UInt;
        values[insertAt + 1].Type = AtkValueType.UInt;
        values[insertAt + 1].UInt = rowKind;
        values[insertAt + 2].Type = AtkValueType.UInt;
        values[insertAt + 3].Type = AtkValueType.Int;
        values[insertAt + 3].Int = -1;
        values[insertAt + 4].SetManagedString(col1);
        values[insertAt + 5].SetManagedString(col2);
        values[insertAt + 6].Type = AtkValueType.Int;
        values[insertAt + 7].SetManagedString("");

        values[2].UInt = (uint)(count + 1);
        return true;
    }

    private static void Section(string title)
    {
        ImGui.Spacing();
        ImGui.TextDisabled(title);
        ImGui.Separator();
    }

    private static void Field(string label)
    {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
    }

    private void DrawEstates(ulong cid, EstateLabelCharacter character)
    {
        Section("Your estates");
        if (character.Estates.Count == 0)
        {
            ImGui.TextDisabled("Open the Teleport menu once to load your estates.");
            return;
        }

        if (!ImGui.BeginTable($"##estates{cid}", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit)) return;
        ImGui.TableSetupColumn("##order");
        ImGui.TableSetupColumn("##location");
        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##forget");

        var estates = character.Estates.OrderBy(e => e.Value.Order).ToList();
        for (var j = 0; j < estates.Count; j++)
        {
            var (houseId, estate) = estates[j];
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.BeginDisabled(j == 0);
            if (ImGui.ArrowButton($"##up{houseId}", ImGuiDir.Up)) Move(estates, j, -1);
            ImGui.EndDisabled();
            ImGui.SameLine(0, 2);
            ImGui.BeginDisabled(j == estates.Count - 1);
            if (ImGui.ArrowButton($"##down{houseId}", ImGuiDir.Down)) Move(estates, j, +1);
            ImGui.EndDisabled();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(estate.Location);

            ImGui.TableNextColumn();
            var label = estate.Label;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputTextWithHint($"##label{houseId}", "No label", ref label, 64)) estate.Label = label;
            if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();

            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton($"##forget{houseId}", FontAwesomeIcon.Trash))
            {
                character.Estates.Remove(houseId);
                configuration.Save();
            }
            ImGuiUtil.Tooltip("Forget this estate and its label. Estates you still have come back next time Teleport opens.");
        }
        ImGui.EndTable();
    }

    private void DrawBookmarks(ulong cid, EstateLabelCharacter character)
    {
        Section("Bookmarks");
        if (character.Bookmarks.Count == 0)
            ImGui.TextDisabled("No bookmarks yet. They appear at the end of Residential Areas.");
        else if (ImGui.BeginTable($"##bookmarks{cid}", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("##order");
            ImGui.TableSetupColumn("##location");
            ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##remove");

            (int From, int To)? move = null;

            for (var j = 0; j < character.Bookmarks.Count; j++)
            {
                var b = character.Bookmarks[j];
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.BeginDisabled(j == 0);
                if (ImGui.ArrowButton($"##bmup{cid}-{j}", ImGuiDir.Up)) move = (j, j - 1);
                ImGui.EndDisabled();
                ImGui.SameLine(0, 2);
                ImGui.BeginDisabled(j == character.Bookmarks.Count - 1);
                if (ImGui.ArrowButton($"##bmdown{cid}-{j}", ImGuiDir.Down)) move = (j, j + 1);
                ImGui.EndDisabled();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(FormatBookmark(b));

                ImGui.TableNextColumn();
                var label = b.Label;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText($"##bmlabel{cid}-{j}", ref label, 64)) b.Label = label;
                if (ImGui.IsItemDeactivatedAfterEdit()) configuration.Save();

                ImGui.TableNextColumn();
                if (ImGuiComponents.IconButton($"##bmremove{cid}-{j}", FontAwesomeIcon.Trash))
                {
                    character.Bookmarks.RemoveAt(j);
                    configuration.Save();
                    break;
                }
                ImGuiUtil.Tooltip("Remove this bookmark.");
            }
            ImGui.EndTable();

            if (move is var (from, to))
            {
                (character.Bookmarks[from], character.Bookmarks[to]) = (character.Bookmarks[to], character.Bookmarks[from]);
                configuration.Save();
            }
        }

        DrawAddBookmark(cid, character);
    }

    private void DrawWorldCombo(string id)
    {
        var preview = newWorld == 0 ? "Select a world" : ExcelWorldHelper.GetName(newWorld);
        if (!ImGui.BeginCombo(id, preview, ImGuiComboFlags.HeightLarge)) return;

        var appearing = ImGui.IsWindowAppearing();
        if (appearing)
        {
            worldFilter = "";
            ImGui.SetKeyboardFocusHere();
        }
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##worldfilter", "Search...", ref worldFilter, 32);

        worldTree ??= Enum.GetValues<ExcelWorldHelper.Region>()
                          .Select(r => (r.ToString(), ExcelWorldHelper.GetDataCenters(r, checkForPublicWorlds: true).Select(dc => (dc.Name.ExtractText(), ExcelWorldHelper.GetPublicWorlds(dc.RowId).Select(w => (w.RowId, w.Name.ExtractText())).OrderBy(w => w.Item2).ToArray())).ToArray())).ToArray();

        var focus = newWorld != 0 ? newWorld : playerState.HomeWorld.RowId;
        var filtering = worldFilter.Length > 0;

        foreach (var (region, dataCenters) in worldTree)
        {
            var visible = dataCenters
                          .Select(dc => (dc.DataCenter, Worlds: dc.Worlds.Where(w => w.Name.Contains(worldFilter, StringComparison.OrdinalIgnoreCase)).ToArray()))
                          .Where(dc => dc.Worlds.Length > 0)
                          .ToArray();
            if (visible.Length == 0) continue;

            if (filtering) ImGui.SetNextItemOpen(true);
            else if (appearing) ImGui.SetNextItemOpen(visible.Any(dc => dc.Worlds.Any(w => w.Id == focus)));
            if (!ImGui.TreeNode(region)) continue;

            foreach (var (dataCenter, worlds) in visible)
            {
                if (filtering) ImGui.SetNextItemOpen(true);
                else if (appearing) ImGui.SetNextItemOpen(worlds.Any(w => w.Id == focus));
                if (!ImGui.TreeNode(dataCenter)) continue;

                foreach (var (worldId, name) in worlds)
                {
                    ImGui.Bullet();
                    ImGui.SameLine();
                    if (ImGui.Selectable($"{name}##{worldId}", worldId == newWorld)) newWorld = worldId;
                    if (appearing && worldId == focus) ImGui.SetScrollHereY();
                }
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }
        ImGui.EndCombo();
    }

    private void DrawAddBookmark(ulong cid, EstateLabelCharacter character)
    {
        Section("Add a bookmark");

        var cities = CityDistricts.Keys.ToArray();
        var territories = dataManager.GetExcelSheet<TerritoryType>();
        var names = cities.Select(c => territories.GetRow(CityDistricts[c]).PlaceName.Value.Name.ExtractText()).ToArray();

        if (ImGui.BeginTable($"##addbm{cid}", 4, ImGuiTableFlags.SizingStretchProp, new Vector2(480 * ImGuiHelpers.GlobalScale, 0)))
        {
            ImGui.TableSetupColumn("##l1", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##i1", ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableSetupColumn("##l2", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##i2", ImGuiTableColumnFlags.WidthStretch, 1f);

            ImGui.TableNextRow();
            Field("Label");
            ImGui.InputTextWithHint($"##newlabel{cid}", "Label", ref newLabel, 64);
            Field("World");
            DrawWorldCombo($"##newworld{cid}");

            ImGui.TableNextRow();
            Field("District");
            ImGui.Combo($"##newcity{cid}", ref newCity, names);
            Field("Ward");
            ImGui.InputInt($"##newward{cid}", ref newWard, 1);

            ImGui.TableNextRow();
            if (newIsApartment)
            {
                Field("Room");
                ImGui.InputInt($"##newroom{cid}", ref newRoom, 1);
            }
            else
            {
                Field("Plot");
                ImGui.InputInt($"##newplot{cid}", ref newPlot, 1);
            }
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.Checkbox($"Apartment##newapt{cid}", ref newIsApartment);
            if (newIsApartment)
            {
                ImGui.SameLine();
                ImGui.Checkbox($"Subdivision##newsub{cid}", ref newSub);
            }

            ImGui.EndTable();
        }
        newWard = Math.Clamp(newWard, 1, 30);
        newPlot = Math.Clamp(newPlot, 1, 60);
        newRoom = Math.Clamp(newRoom, 1, 90);

        if (ImGui.Button($"Add bookmark##add{cid}")) AddBookmark(character, cities[newCity]);
        if (addError != null)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), addError);
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

            ImGui.Indent();
            DrawEstates(cid, character);
            DrawBookmarks(cid, character);
            ImGui.Unindent();
        }
    }
}
