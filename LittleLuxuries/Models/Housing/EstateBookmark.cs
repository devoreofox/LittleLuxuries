using System;

namespace LittleLuxuries.Models.Housing;

[Serializable]
public class EstateBookmark
{
    public string Label { get; set; } = "";
    public int World { get; set; }
    public int City { get; set; } = 8;
    public int Ward { get; set; } = 1;
    public bool IsApartment { get; set; }
    public int Plot { get; set; } = 1;
    public int Apartment { get; set; } = 1;
    public bool ApartmentSubdivision { get; set; }
}
