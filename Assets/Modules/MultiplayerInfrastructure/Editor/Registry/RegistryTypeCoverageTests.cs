using System;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.RegistryStores
{
  /// <summary>
  /// RegistryType 값마다 실제 저장소가 있는지 검사한다.
  ///
  /// <para>
  /// Registry 는 종류를 저장소로 바꿀 때 빠짐없는 switch 를 쓰고, 모르는 값이면 예외를 던진다. 열거형에
  /// 값만 추가하고 저장소와 분기를 빠뜨리면 컴파일은 통과하지만 그 종류를 처음 읽거나 쓰는 순간
  /// 예외가 난다. 그 호출이 NetworkBehaviour 의 스폰 콜백 안에 있으면 FishNet 의 콜백 순회가 거기서
  /// 끊겨, 뒤에 있는 컴포넌트가 초기화되지 않은 채 살아 움직이는 상태가 된다.
  /// </para>
  /// </summary>
  public sealed class RegistryTypeCoverageTests
  {
    [Test]
    public void EveryRegistryTypeHasABackingStore()
    {
      foreach (RegistryType registryType in Enum.GetValues(typeof(RegistryType)))
      {
        string identifier = $"__registry_type_coverage_{registryType}";
        Assert.DoesNotThrow(
          () =>
          {
            Registry.Registry.TryGet<object>(registryType, identifier, out _);
            Registry.Registry.Register(registryType, identifier, new object());
            Registry.Registry.Unregister(registryType, identifier);
          },
          $"RegistryType.{registryType} 에 대응하는 저장소가 없습니다. "
          + "Registry 의 저장소 필드와 ResolveRegistry 분기를 함께 추가해야 합니다.");
      }
    }
  }
}
