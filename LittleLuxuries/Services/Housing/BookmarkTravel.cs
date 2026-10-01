using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using LittleLuxuries.Models.Housing;

namespace LittleLuxuries.Services.Housing;

public sealed unsafe class BookmarkTravel
{
    private readonly IPlayerState playerState;
    private readonly IChatGui chatGui;
    private readonly IFramework framework;
    private readonly Configuration configuration;
    private readonly EstateAddresses addresses;
    private readonly ICallGateSubscriber<(string, int, int, int, int, int, int, bool, bool, string), object> lifestreamGoTo;
    private readonly ICallGateSubscriber<bool> lifestreamBusy;

    public BookmarkTravel(IPlayerState playerState, IChatGui chatGui, IFramework framework, Configuration configuration, EstateAddresses addresses)
    {
        this.playerState = playerState;
        this.chatGui = chatGui;
        this.framework = framework;
        this.configuration = configuration;
        this.addresses = addresses;

        lifestreamGoTo = Plugin.PluginInterface.GetIpcSubscriber<(string, int, int, int, int, int, int, bool, bool, string), object>("Lifestream.GoToHousingAddress");
        lifestreamBusy = Plugin.PluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
    }

    public bool LifestreamAvailable()
    {
        try
        {
            lifestreamBusy.InvokeFunc();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Go(EstateBookmark bookmark)
    {
        framework.RunOnTick(() =>
        {
            CloseTeleportWindow();
            Travel(bookmark);
        });
    }

    private static void CloseTeleportWindow()
    {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName("Teleport");
        if (addon != null && addon->IsVisible) addon->Close(true);
    }

    private void Travel(EstateBookmark b)
    {
        try
        {
            if (lifestreamBusy.InvokeFunc())
            {
                chatGui.PrintError("Lifestream is busy with another trip.");
                return;
            }
            lifestreamGoTo.InvokeAction((b.Label, b.World, b.City, b.Ward, b.IsApartment ? 1 : 0, b.Plot, b.Apartment, b.ApartmentSubdivision, false, ""));
            return;
        }
        catch (IpcNotReadyError)
        {
        }

        var name = string.IsNullOrWhiteSpace(b.Label) ? "your estate" : b.Label;
        var sameWorld = playerState.CurrentWorld.RowId == (uint)b.World;
        var aetheryte = (uint)(sameWorld ? b.City : configuration.EstateBookmarkTravelCity);
        if (!Telepo.Instance()->Teleport(aetheryte, 0))
        {
            chatGui.PrintError($"Couldn't teleport toward {name}.");
            return;
        }

        chatGui.Print(sameWorld
            ? $"Teleporting toward {name} ({addresses.FormatBookmark(b)}). Take the aethernet from there."
            : $"{name} is on another world ({addresses.FormatBookmark(b)}). Teleporting so you can world visit.");
    }
}
