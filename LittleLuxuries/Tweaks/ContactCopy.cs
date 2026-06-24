using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using LittleLuxuries.UI;

namespace LittleLuxuries.Tweaks;

public sealed class ContactCopy : Tweak, IDisposable
{
    private readonly IContextMenu contextMenu;
    private readonly Configuration configuration;

    public ContactCopy(IContextMenu contextMenu, Configuration configuration)
    {
        this.contextMenu = contextMenu;
        this.configuration = configuration;
        contextMenu.OnMenuOpened += OnMenuOpened;
    }

    public override string Name => "Contact Copy";
    public override string Description => "Adds a \"Copy Name\" option to the right-click menu in the Contact List, copying a player's Name@World to the clipboard.";
    public override bool IsImplemented => true;

    public void Dispose() => contextMenu.OnMenuOpened -= OnMenuOpened;

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (!configuration.CopyContactNames) return;
        if (args.AddonName != "ContactList") return;
        if (args.Target is not MenuTargetDefault target || string.IsNullOrEmpty(target.TargetName)) return;

        args.AddMenuItem(new MenuItem
        {
            Name = "Copy Name",
            Prefix = SeIconChar.BoxedLetterL,
            PrefixColor = 541,
            OnClicked = _ =>
            {
                var name = target.TargetName;
                if (configuration.CopyContactWithWorld) name += "@" + target.TargetHomeWorld.Value.Name.ExtractText();
                ImGui.SetClipboardText(name);
            }
        });
    }

    public override void DrawConfig()
    {
        var enabled = configuration.CopyContactNames;
        if (ImGui.Checkbox("Copy names from the Contact List", ref enabled))
        {
            configuration.CopyContactNames = enabled;
            configuration.Save();
        }
        ImGuiUtil.Tooltip("Adds a \"Copy Name\" entry when you right-click someone in the Contact List, copying their name to your clipboard.");

        if (configuration.CopyContactNames)
        {
            var withWorld = configuration.CopyContactWithWorld;
            if (ImGui.Checkbox("Include Homeworld", ref withWorld))
            {
                configuration.CopyContactWithWorld = withWorld;
                configuration.Save();
            }
            ImGuiUtil.Tooltip("Copy \"Name@World\" instead of just the name.");
        }

        ImGui.Spacing();
        ImGui.TextWrapped("Right-click a player in the Contact List and choose \"Copy Name\".");
    }
}
