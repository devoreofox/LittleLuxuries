using System;
using System.Collections.Generic;

namespace LittleLuxuries.Models.Housing;

[Serializable]
public class EstateLabelCharacter
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<ulong, EstateLabel> Estates { get; set; } = new();
}
