using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;

namespace Vosk
{
    public class VoskLoader
    {
        [DllImport("__Internal")]
        private static extern void LoadVosk();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (Application.isEditor)
            {
                var pluginsPath = Path.Combine(Application.dataPath, "Plugins", "macOS");
                var libraryPath = Path.Combine(pluginsPath, "libvosk.dylib");
                Debug.Log($"Loading Vosk from: {libraryPath}");
                if (!File.Exists(libraryPath))
                {
                    Debug.LogError($"Vosk library not found at {libraryPath}");
                    return;
                }
                // On macOS, the dylib should be loaded automatically if it's in the right place
                Debug.Log("Vosk library found. It should be loaded automatically.");
            }
            else
            {
                LoadVosk();
            }
#endif
        }
    }
}
