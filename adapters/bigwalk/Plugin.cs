using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace XRT.BigWalk;

[BepInPlugin("dev.nowloady.xrt.bigwalk", "XRTranslate · Big Walk", "0.1.0")]
public sealed class Plugin : BasePlugin
{
    private static ChatBridge? bridge;
    private readonly Harmony harmony = new("dev.nowloady.xrt.bigwalk");

    public override void Load()
    {
        bridge = new ChatBridge(Log);
        try
        {
            var chat = new NativeChat();
            harmony.Patch(chat.UpdateMethod, postfix: new HarmonyMethod(typeof(Plugin), nameof(Tick)));
            bridge.Bind(chat);
        }
        catch (Exception exception)
        {
            bridge.BindingError = "The game's chat interface is unavailable.";
            Log.LogError($"Cannot bind native chat: {exception.Message}");
        }
        bridge.Start();
    }

    private static void Tick() => bridge?.Tick();

    public override bool Unload()
    {
        bridge?.Dispose();
        bridge = null;
        harmony.UnpatchSelf();
        return true;
    }
}
