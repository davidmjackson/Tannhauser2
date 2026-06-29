using UnityEngine;
using UnityEngine.InputSystem;

// Press Esc to quit the standalone build.
// Auto-spawns at runtime, so it needs no GameObject in the scene.
// In the Editor, Application.Quit() does nothing, so this is build-only behaviour.
public class QuitHandler : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        var go = new GameObject("QuitHandler");
        go.AddComponent<QuitHandler>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
    }
}
