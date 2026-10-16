using System.Reflection;

namespace XRT.BigWalk;

// Bind only the normal client command. Game assemblies stay in the game install.
internal sealed class NativeChat
{
    private readonly PropertyInfo connected;
    private readonly PropertyInfo localPlayer;
    private readonly PropertyInfo pointer;
    private readonly MethodInfo getComponent;
    private readonly MethodInfo send;
    private readonly MethodInfo alive;
    private IntPtr cachedIdentity;
    private object? cachedPlayer;
    internal MethodInfo UpdateMethod { get; }

    internal NativeChat()
    {
        var network = Type.GetType("Mirror.NetworkClient, Mirror", throwOnError: true)!;
        var player = Type.GetType("PlayerNetworking, Assembly-CSharp", throwOnError: true)!;
        var unityObject = Type.GetType("UnityEngine.Object, UnityEngine.CoreModule", throwOnError: true)!;
        connected = network.GetProperty("isConnected") ?? throw new MissingMemberException("NetworkClient.isConnected");
        localPlayer = network.GetProperty("localPlayer") ?? throw new MissingMemberException("NetworkClient.localPlayer");
        pointer = localPlayer.PropertyType.GetProperty("Pointer") ?? throw new MissingMemberException("NetworkIdentity.Pointer");
        getComponent = localPlayer.PropertyType.GetMethods().Single(method =>
            method.Name == "GetComponent" && method.IsGenericMethodDefinition &&
            method.GetParameters().Length == 0).MakeGenericMethod(player);
        send = player.GetMethod("CmdSendTextChatMessage", new[] { typeof(string) })
            ?? throw new MissingMethodException("PlayerNetworking.CmdSendTextChatMessage");
        alive = unityObject.GetMethod("op_Implicit", new[] { unityObject })
            ?? throw new MissingMethodException("UnityEngine.Object.op_Implicit");
        UpdateMethod = network.GetMethod("NetworkEarlyUpdate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException("NetworkClient.NetworkEarlyUpdate");
    }

    internal (object? Player, IntPtr Identity) LocalPlayer()
    {
        if (connected.GetValue(null) is not true) return Unavailable();
        var identity = localPlayer.GetValue(null);
        if (identity == null || alive.Invoke(null, new[] { identity }) is not true) return Unavailable();
        var identityPointer = (IntPtr)pointer.GetValue(identity)!;
        if (cachedIdentity != identityPointer || cachedPlayer == null)
        {
            cachedIdentity = identityPointer;
            cachedPlayer = getComponent.Invoke(identity, null);
        }
        if (cachedPlayer == null || alive.Invoke(null, new[] { cachedPlayer }) is not true) return Unavailable();
        return (cachedPlayer, identityPointer);
    }

    private (object?, IntPtr) Unavailable()
    {
        cachedIdentity = IntPtr.Zero;
        cachedPlayer = null;
        return (null, IntPtr.Zero);
    }

    internal void Send(object player, string text) => send.Invoke(player, new object[] { text });
}
