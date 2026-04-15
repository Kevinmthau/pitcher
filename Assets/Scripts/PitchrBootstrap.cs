using UnityEngine;

namespace Pitchr
{
    internal static class PitchrBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntime()
        {
            if (Object.FindAnyObjectByType<PitchrRuntime>() != null)
            {
                return;
            }

            var runtime = new GameObject("PitchrRuntime");
            runtime.AddComponent<PitchrRuntime>();
        }
    }
}
