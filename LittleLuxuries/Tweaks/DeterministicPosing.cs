using Dalamud.Bindings.ImGui;
using LittleLuxuries.Services.Dpose;

namespace LittleLuxuries.Tweaks;

public class DeterministicPosing : Tweak
{
    private readonly CposeController controller;
    public DeterministicPosing(CposeController controller) => this.controller = controller;
    public override string Name => "Deterministic Posing";
    public override string Description => "Extends the /cpose command to accept an index, allowing you to jump directly to a specific pose rather than cycling through them one at a time. For example, /cpose 3 immediately sets your third standing pose.";
    public override bool IsImplemented => true;

    public override void DrawConfig()
    {
        var type = controller.GetCurrentPoseType();
        var pose = controller.GetCurrentPose();

        ImGui.Text($"Pose Type: {type.ToString() ?? "None"}");
        ImGui.Text($"Current Index: {pose?.ToString() ?? "None"}");
        if (type is not null) ImGui.Text($"Available: 0 - {controller.GetMaxPose(type.Value)}");

        if (type is not null)
        {
            var max = controller.GetMaxPose(type.Value);
            ImGui.Text($"Available: 0 - {max}");
            for (byte i = 0; i <= max; i++)
            {
                if (i > 0) ImGui.SameLine();
                var idx = i;
                if (ImGui.Button($"{idx}")) controller.DriveTo(idx);
            }
        }
    }
}
