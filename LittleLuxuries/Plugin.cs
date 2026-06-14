using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons;
using LittleLuxuries.Services.Dpose;
using LittleLuxuries.Services.Housing;
using LittleLuxuries.Tweaks;
using LittleLuxuries.Windows;

namespace LittleLuxuries;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static INamePlateGui NamePlateGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/llux";

    private readonly FurnishingScanner _scanner;

    private readonly HousingArrowHider _housingArrowHider;
    private readonly DeterministicPosing? _deterministicPosing;

    public Configuration Configuration { get; init; }
    public List<Tweak> Tweaks { get; } = new();

    public readonly WindowSystem WindowSystem = new("Little Luxuries");
    private MainWindow MainWindow { get; init; }
    private ArrowWhitelistWindow _arrowWhitelistWindow = null!;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ECommonsMain.Init(PluginInterface, this);

        MainWindow = new MainWindow(this);
        _scanner = new FurnishingScanner(ObjectTable);

        var cpose = new CposeController(ClientState, Framework);

        var housingArrowHider = new HousingArrowHider(NamePlateGui, ClientState, GameGui, Framework, _scanner, () => _arrowWhitelistWindow.Toggle(), Configuration);
        var deterministicPosing = new DeterministicPosing(cpose, Configuration, ChatGui, GameInterop);

        _arrowWhitelistWindow = new ArrowWhitelistWindow(housingArrowHider, _scanner);
        _housingArrowHider = housingArrowHider;

        Tweaks.Add(housingArrowHider);
        Tweaks.Add(new PersonalEstateLabels());
        Tweaks.Add(new PartyFinderCleanup());
        Tweaks.Add(deterministicPosing);
        _deterministicPosing = deterministicPosing;
        Tweaks.Add(new CharacterSelectTweaks());

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(_arrowWhitelistWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "/llux → Open the Little Luxuries tweak window"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();

        foreach (var tweak in Tweaks) (tweak as IDisposable)?.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void ToggleMainUi() => MainWindow.Toggle();
}
