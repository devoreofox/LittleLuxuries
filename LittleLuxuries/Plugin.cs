using System;
using System.Collections.Generic;
using System.Reflection;
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
    [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    private const string CommandName = "/llux";

    private readonly FurnishingScanner _scanner;

    public Configuration Configuration { get; init; }
    public List<Tweak> Tweaks { get; } = new();

    public readonly WindowSystem WindowSystem = new("Little Luxuries");
    private MainWindow MainWindow { get; init; }
    private ArrowWhitelistWindow _arrowWhitelistWindow = null!;
    private ChangelogWindow ChangelogWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ECommonsMain.Init(PluginInterface, this);

        MainWindow = new MainWindow(this, ToggleChangelogUi);
        ChangelogWindow = new ChangelogWindow();
        _scanner = new FurnishingScanner(ObjectTable);

        var cpose = new CposeController(ClientState, Framework);

        var housingArrowHider = new HousingArrowHider(NamePlateGui, ClientState, GameGui, Framework, _scanner, () => _arrowWhitelistWindow.Toggle(), Configuration);
        var deterministicPosing = new DeterministicPosing(cpose, Configuration, ChatGui, GameInterop);

        _arrowWhitelistWindow = new ArrowWhitelistWindow(housingArrowHider, _scanner);

        var estateAccess = new EstateAccessController(ClientState, Condition, AddonLifecycle, GameInterop);

        Tweaks.Add(housingArrowHider);
        Tweaks.Add(new PersonalEstateLabels());
        Tweaks.Add(new PartyFinderCleanup());
        Tweaks.Add(deterministicPosing);
        Tweaks.Add(new CharacterSelectTweaks());
        Tweaks.Add(new ContactCopy(ContextMenu, Configuration));
        Tweaks.Add(new EstateKey(estateAccess, Configuration, CommandManager, ChatGui));
        Tweaks.Add(new CommendQueue());
        Tweaks.Add(new BlindFaith());

        if (!Configuration.NewTweaksInitialized)
        {
            var newThisRelease = new HashSet<string> { "Estate Key" }; //Remove on next release (please don't forget Oreo, god x-x) Yes this is for you, whoever is reading these. >:(

            foreach (var tweak in Tweaks)
            {
                if (!newThisRelease.Contains(tweak.Name)) Configuration.NewTweaks.Add(tweak.Name);
            }
            Configuration.NewTweaksInitialized = true;
            Configuration.Save();
        }

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(_arrowWhitelistWindow);
        WindowSystem.AddWindow(ChangelogWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "/llux → Open the Little Luxuries tweak window.\n" +
                          "/llux changelog → Open the Little Luxuries changelog window."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        var current = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        if (Configuration.LastSeenVersion != current)
        {
            ChangelogWindow.IsOpen = true;
            Configuration.LastSeenVersion = current;
            Configuration.Save();
        }
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();
        ChangelogWindow.Dispose();

        foreach (var tweak in Tweaks) (tweak as IDisposable)?.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "changelog": ToggleChangelogUi(); break;
            default: MainWindow.Toggle(); break;
        }
    }

    public void ToggleMainUi() => MainWindow.Toggle();
    public void ToggleChangelogUi() => ChangelogWindow.Toggle();
}
