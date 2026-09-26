using System;

namespace LittleLuxuries.Models.Housing;

[Serializable]
public class EstateLabel
{
   public string Location { get; set; } = string.Empty;
   public string Label { get; set; } = string.Empty;
   public int Order { get; set; }
}
