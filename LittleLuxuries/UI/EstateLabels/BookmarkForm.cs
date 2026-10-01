using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using LittleLuxuries.Models.Housing;
using LittleLuxuries.Services.Housing;

namespace LittleLuxuries.UI.EstateLabels;

public sealed class BookmarkForm
{
    private readonly Configuration configuration;
    private readonly EstateAddresses addresses;
    private readonly WorldPicker worldPicker;

    private string newLabel = "";
    private uint newWorld;
    private string? addError;
    private int newCity, newWard = 1, newPlot = 1, newRoom = 1;
    private bool newIsApartment, newSub;

    public BookmarkForm(Configuration configuration, EstateAddresses addresses, WorldPicker worldPicker)
    {
        this.configuration = configuration;
        this.addresses = addresses;
        this.worldPicker = worldPicker;
    }

    public void Draw(ulong cid, EstateLabelCharacter character)
    {
        ImGuiUtil.Section("Add a bookmark");

        var cities = EstateAddresses.CityDistricts.Keys.ToArray();
        var names = cities.Select(addresses.DistrictName).ToArray();

        if (ImGui.BeginTable($"##addbm{cid}", 4, ImGuiTableFlags.SizingStretchProp, new Vector2(480 * ImGuiHelpers.GlobalScale, 0)))
        {
            ImGui.TableSetupColumn("##l1", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##i1", ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableSetupColumn("##l2", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##i2", ImGuiTableColumnFlags.WidthStretch, 1f);

            ImGui.TableNextRow();
            ImGuiUtil.Field("Label");
            ImGui.InputTextWithHint($"##newlabel{cid}", "Label", ref newLabel, 64);
            ImGuiUtil.Field("World");
            worldPicker.Draw($"##newworld{cid}", ref newWorld);

            ImGui.TableNextRow();
            ImGuiUtil.Field("District");
            ImGui.Combo($"##newcity{cid}", ref newCity, names);
            ImGuiUtil.Field("Ward");
            ImGui.InputInt($"##newward{cid}", ref newWard, 1);

            ImGui.TableNextRow();
            if (newIsApartment)
            {
                ImGuiUtil.Field("Room");
                ImGui.InputInt($"##newroom{cid}", ref newRoom, 1);
            }
            else
            {
                ImGuiUtil.Field("Plot");
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

        if (ImGui.Button($"Add bookmark##add{cid}")) Add(character, cities[newCity]);
        if (addError != null)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), addError);
        }
    }

    private void Add(EstateLabelCharacter character, int city)
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
}
