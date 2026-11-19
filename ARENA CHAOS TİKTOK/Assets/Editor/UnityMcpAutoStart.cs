#if UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using UnityEditor;

[InitializeOnLoad]
internal static class UnityMcpAutoStart
{
    private const string UvxPathPreference = "MCPForUnity.UvxPath";

    static UnityMcpAutoStart()
    {
        EditorApplication.delayCall += StartMcpAfterEditorLoad;
    }

    private static async void StartMcpAfterEditorLoad()
    {
        EditorApplication.delayCall -= StartMcpAfterEditorLoad;

        string uvxPath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Tools", "uvx.cmd"));

        if (File.Exists(uvxPath))
        {
            EditorPrefs.SetString(UvxPathPreference, uvxPath);
        }

        try
        {
            if (!MCPServiceLocator.Server.IsLocalHttpServerReachable())
            {
                MCPServiceLocator.Server.StartLocalHttpServer(quiet: true);
            }

            await Task.Delay(1200);

            if (!MCPServiceLocator.Bridge.IsRunning)
            {
                await MCPServiceLocator.Bridge.StartAsync();
            }
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogWarning("Unity MCP otomatik başlatılamadı: " + exception.Message);
        }
    }
}
#endif
