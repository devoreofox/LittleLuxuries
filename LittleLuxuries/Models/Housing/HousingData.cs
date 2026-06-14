using System.Collections.Generic;

namespace LittleLuxuries.Models.Housing;

public static class HousingData
{
    public static readonly HashSet<uint> TerritoryIds = new()
    {
        282, 283, 284, 384, 608,
        342, 343, 344, 385, 609,
        345, 346, 347, 386, 610,
        649, 650, 651, 652, 655,
        980, 981, 982, 983, 999,
        1249, 1250, 1251,
        1374, 1375, 1376,
    };

    //TODO add localization support, think furnishing id instead of name? Need to find actual usable ids for this
    //TODO poll and add common items here, these should be common things that people want to easily manipulate
    public static readonly string[] InteractiveFurnishings =
    {
        "Armoire",
        "Company Chest",
        "Crystal Bell",
        "Message Book Stand",
        "Orchestrion",
        "Orchestrion Phonograph",
        "Summoning Bell",
        "Table Orchestrion",
        "Triple Triad Board",
    };
}
