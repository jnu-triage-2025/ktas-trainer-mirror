using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    private const string IconResourceRoot = "Textures/Icons";

    private static readonly (string Identifier, string Path)[] IconLiterals =
    {
      ("message-circle", $"{IconResourceRoot}/message-circle")
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapBuiltInRegistry()
    {
      EnsureBuiltInRegistryInitialized();
    }

    static partial void RegisterBuiltInLiterals()
    {
      RegisterIconLiterals();
    }

    private static void RegisterIconLiterals()
    {
      for (int i = 0; i < IconLiterals.Length; i++)
      {
        var entry = IconLiterals[i];

        string resourcePath = entry.Path;
        if (Resources.Load<Sprite>(resourcePath) == null)
        {
          Debug.LogWarning($"[Registry] Failed to load icon sprite '{entry.Identifier}' from '{resourcePath}'.");
          continue;
        }

        Register(RegistryType.IconSprite, entry.Identifier, resourcePath);
      }
    }
  }
}
