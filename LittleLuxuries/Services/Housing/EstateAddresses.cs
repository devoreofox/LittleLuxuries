using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using LittleLuxuries.Models.Housing;
using Lumina.Excel.Sheets;

namespace LittleLuxuries.Services.Housing;

public sealed class EstateAddresses
{
    public static readonly Dictionary<int, ushort> CityDistricts = new() { [8] = 339, [2] = 340, [9] = 341, [111] = 641, [70] = 979 };

    private readonly IDataManager dataManager;
    private readonly Configuration configuration;

    public EstateAddresses(IDataManager dataManager, Configuration configuration)
    {
        this.dataManager = dataManager;
        this.configuration = configuration;
    }

    public string PlaceName(uint territory) => dataManager.GetExcelSheet<TerritoryType>().GetRow(territory).PlaceName.Value.Name.ExtractText();

    public string DistrictName(int city) => PlaceName(CityDistricts[city]);

    public string FormatLocation(HouseId h, string placeName) =>
        Compose(h.WorldId, placeName, h.WardIndex + 1, Unit(h.IsApartment, h.ApartmentDivision == 1, h.RoomNumber, h.PlotIndex + 1),
                configuration.EstateLabelsWorld, configuration.EstateLabelsDistrict, configuration.EstateLabelsWard, configuration.EstateLabelsPlot);

    public string FormatBookmark(EstateBookmark b, bool full = false) =>
        Compose(b.World, DistrictName(b.City), b.Ward, Unit(b.IsApartment, b.ApartmentSubdivision, b.Apartment, b.Plot),
                full || configuration.EstateBookmarkWorld, full || configuration.EstateBookmarkDistrict,
                full || configuration.EstateBookmarkWard, full || configuration.EstateBookmarkPlot);

    private static string Unit(bool isApartment, bool subdivision, int room, int plot) =>
        isApartment ? $"{(subdivision ? "Sub " : "")}Apt {room}" : $"P{plot}";

    private string Compose(int world, string district, int ward, string unit, bool showWorld, bool showDistrict, bool showWard, bool showPlot)
    {
        var name = string.Join(", ", new[]
        {
            showWorld ? dataManager.GetExcelSheet<World>().GetRow((uint)world).Name.ExtractText() : null,
            showDistrict ? district : null,
        }.OfType<string>());

        var numbers = string.Join(" ", new[]
        {
            showWard ? $"W{ward}" : null,
            showPlot ? unit : null,
        }.OfType<string>());

        if (numbers.Length == 0) return name;
        return name.Length == 0 ? numbers : $"{name} ({numbers})";
    }

    public static EstateBookmark? AsBookmark(ulong houseId, EstateLabel estate)
    {
        var h = new HouseId { Id = houseId };
        var city = CityDistricts.FirstOrDefault(d => d.Value == h.TerritoryTypeId).Key;
        if (city == 0) return null;

        return new EstateBookmark
        {
            Label = estate.Label, World = h.WorldId, City = city, Ward = h.WardIndex + 1,
            IsApartment = h.IsApartment, Plot = h.PlotIndex + 1, Apartment = h.RoomNumber, ApartmentSubdivision = h.ApartmentDivision == 1,
        };
    }
}
