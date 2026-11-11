using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LittleLuxuries.Models.Housing;
using Lumina.Excel.Sheets;

namespace LittleLuxuries.Services.Housing;

public sealed unsafe class TeleportListEditor : IDisposable
{
    private const uint HousingIcon = 0x50000, RowFlag = 1;
    private const uint RegionResidentialHeader = 0x900, ExpansionResidentialHeader = 0x200;
    private const uint ResidentialAreasAddonRow = 8495;

    public const string DefaultDirectHeading = "Direct Teleport", DefaultBookmarkHeading = "Bookmarks";

    private readonly IAddonLifecycle addonLifecycle;
    private readonly IDataManager dataManager;
    private readonly IPlayerState playerState;
    private readonly Configuration configuration;
    private readonly EstateRegistry registry;
    private readonly EstateAddresses addresses;
    private readonly BookmarkTravel travel;

    private readonly Dictionary<uint, EstateBookmark> bookmarkBlocks = new();

    public TeleportListEditor(IAddonLifecycle addonLifecycle, IDataManager dataManager, IPlayerState playerState, Configuration configuration,
                              EstateRegistry registry, EstateAddresses addresses, BookmarkTravel travel)
    {
        this.addonLifecycle = addonLifecycle;
        this.dataManager = dataManager;
        this.playerState = playerState;
        this.configuration = configuration;
        this.registry = registry;
        this.addresses = addresses;
        this.travel = travel;

        addonLifecycle.RegisterListener(AddonEvent.PreSetup, "Teleport", OnTeleportPreSetup);
        addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "Teleport", OnTeleportReceiveEvent);
    }

    public void Dispose() => addonLifecycle.UnregisterListener(OnTeleportPreSetup, OnTeleportReceiveEvent);

    private void OnTeleportPreSetup(AddonEvent type, AddonArgs args)
    {
        bookmarkBlocks.Clear();
        if (!configuration.PersonalEstateLabels) return;

        registry.Refresh();
        if (registry.Current is not { } character) return;

        var setup = (AddonSetupArgs)args;
        var values = (AtkValue*)setup.AtkValues;
        var teleports = Telepo.Instance()->TeleportList.AsSpan();

        RelabelAndReorder(values, setup.AtkValueCount, teleports, character);
        InsertTravelRows(values, setup.AtkValueCount, teleports, character);
    }

    private void RelabelAndReorder(AtkValue* values, uint valueCount, Span<TeleportInfo> teleports, EstateLabelCharacter character)
    {
        var rows = new List<(int Start, int Order)>();

        for (var i = 2; i + 7 < valueCount; i += 8)
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

    private void InsertTravelRows(AtkValue* values, uint valueCount, Span<TeleportInfo> teleports, EstateLabelCharacter character)
    {
        var currentWorld = playerState.CurrentWorld.RowId;
        var travelRows = character.Estates.OrderBy(e => e.Value.Order)
                                  .Select(e => EstateAddresses.AsBookmark(e.Key, e.Value)).OfType<EstateBookmark>()
                                  .Where(b => (uint)b.World != currentWorld)
                                  .Concat(character.Bookmarks)
                                  .ToList();

        var headerKind = values[0].UInt == 1 ? RegionResidentialHeader : ExpansionResidentialHeader;
        var rowKind = HousingIcon | headerKind | RowFlag;
        var hasResidential = values[3].UInt == headerKind;
        var split = configuration.EstateLabelsSplitHeadings;

        if (split && hasResidential) values[7].SetManagedString(HeadingOr(configuration.EstateLabelsDirectHeading, DefaultDirectHeading));

        if (travelRows.Count == 0) return;

        var bookmarkHeading = split
                                  ? HeadingOr(configuration.EstateLabelsBookmarkHeading, DefaultBookmarkHeading)
                                  : dataManager.GetExcelSheet<Addon>().GetRow(ResidentialAreasAddonRow).Text.ExtractText();

        int insertAt;
        if (hasResidential)
        {
            var end = 2 + (int)values[2].UInt * 8;
            insertAt = 10;
            while (insertAt < end && values[insertAt + 3].Int >= 0) insertAt += 8;

            if (split)
            {
                if (!InsertRow(values, valueCount, insertAt, headerKind, "", bookmarkHeading, 0, "")) return;
                insertAt += 8;
            }
        }
        else
        {
            if (!InsertRow(values, valueCount, 2, headerKind, "", bookmarkHeading, 0, "")) return;
            insertAt = 10;
        }

        var labelFirst = configuration.EstateLabelsLabelFirst;

        foreach (var b in travelRows)
        {
            var location = addresses.FormatBookmark(b);

            var sameWorld = currentWorld == (uint)b.World;
            var cost = CostTo(teleports, (uint)(sameWorld ? b.City : configuration.EstateBookmarkTravelCity));
            var costText = cost is { } c ? c.ToString("N0", CultureInfo.InvariantCulture) + "\uE049" : "";

            if (!InsertRow(values, valueCount, insertAt, rowKind, labelFirst ? b.Label : location, labelFirst ? location : b.Label,
                           (int)(cost ?? 0), costText)) break;
            bookmarkBlocks[(uint)((insertAt - 2) / 8)] = b;
            insertAt += 8;
        }
    }

    private void OnTeleportReceiveEvent(AddonEvent type, AddonArgs args)
    {
        var e = (AddonReceiveEventArgs)args;
        if (e.AtkEventType != AddonEventType.ListItemClick) return;

        var data = (AtkEventData*)e.AtkEventData;
        if (data == null || data->ListItemData.ListItem == null || data->ListItemData.MouseButtonId != 0) return;

        var block = data->ListItemData.ListItem->UIntValues.AsSpan()[2];
        if (!bookmarkBlocks.TryGetValue(block, out var bookmark)) return;

        travel.Go(bookmark);
    }

    private static string HeadingOr(string heading, string fallback) => string.IsNullOrWhiteSpace(heading) ? fallback : heading;

    private static uint? CostTo(ReadOnlySpan<TeleportInfo> teleports, uint aetheryte)
    {
        foreach (var t in teleports)
            if (t.AetheryteId == aetheryte && t.SubIndex == 0) return t.GilCost;
        return null;
    }

    private static bool InsertRow(AtkValue* values, uint valueCount, int insertAt, uint rowKind, string col1, string col2, int cost, string costText)
    {
        var count = (int)values[2].UInt;
        var end = 2 + count * 8;
        if (end + 8 >= valueCount || values[end + 8].Type != AtkValueType.Undefined) return false;

        var bytes = (end - insertAt + 1) * sizeof(AtkValue);
        Buffer.MemoryCopy(values + insertAt, values + insertAt + 8, bytes, bytes);

        for (var k = 0; k < 8; k++) values[insertAt + k] = default;

        if (insertAt == 2) values[10].UInt = 0;

        values[insertAt + 0].Type = AtkValueType.UInt;
        values[insertAt + 1].Type = AtkValueType.UInt;
        values[insertAt + 1].UInt = rowKind;
        values[insertAt + 2].Type = AtkValueType.UInt;
        values[insertAt + 3].Type = AtkValueType.Int;
        values[insertAt + 3].Int = -1;
        values[insertAt + 4].SetManagedString(col1);
        values[insertAt + 5].SetManagedString(col2);
        values[insertAt + 6].Type = AtkValueType.Int;
        values[insertAt + 6].Int = cost;
        values[insertAt + 7].SetManagedString(costText);

        values[2].UInt = (uint)(count + 1);
        return true;
    }
}
