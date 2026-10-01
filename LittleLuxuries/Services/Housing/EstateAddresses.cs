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

    public string FormatLocation(HouseId h, string placeName)
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

    public string FormatBookmark(EstateBookmark b)
    {
        var world = dataManager.GetExcelSheet<World>().GetRow((uint)b.World).Name.ExtractText();
        var place = DistrictName(b.City);
        var unit = b.IsApartment ? $"{(b.ApartmentSubdivision ? "Sub " : "")}Apt {b.Apartment}" : $"P{b.Plot}";
        return $"{world}, {place} (W{b.Ward} {unit})";
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
