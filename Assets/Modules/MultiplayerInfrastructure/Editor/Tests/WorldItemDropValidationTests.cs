using System.Reflection;
using MultiplayerInfrastructure.Player;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests
{
  public sealed class WorldItemDropValidationTests
  {
    [Test]
    public void DropValidationRejectsNonFinitePositionBeforeSpawning()
    {
      var playerObject = new GameObject("world-item-drop-validation-player");
      try
      {
        var player = playerObject.AddComponent<PlayerController>();
        var validate = typeof(PlayerController).GetMethod(
          "TryValidateDroppedWorldItemRequest",
          BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(validate, Is.Not.Null);

        object[] arguments =
        {
          "plaster",
          1,
          30,
          0f,
          string.Empty,
          new Vector3(float.NaN, 0f, 0f),
          Quaternion.identity,
          Vector3.zero,
          null
        };

        bool accepted = (bool)validate.Invoke(player, arguments);

        Assert.That(accepted, Is.False);
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
      }
    }
  }
}
