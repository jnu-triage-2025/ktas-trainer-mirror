using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// UIControllerABC는 모든 UI 컨트롤러의 상속이 의도되는 추상 메서드입니다. 각 UI 요소들이 싱글톤 패턴이
  /// 의도되지 않았으므로, 외부에서 UI 컨트롤을 취득하는 데 있어서 별개의 레지스트리로부터 접근할 수 있도록 하고 있습니다.
  ///
  /// UIControllerABC는 레지스트리를 통해 외부에서 접근 가능하도록 이 컨트롤을 레지스터하는 역할을 합니다.
  /// </summary>
  public abstract class UIControllerABC : MonoBehaviour
  {
    protected virtual void Awake()
    {
      Register();
    }

    private void Register()
    {
      Registry.Registry.Register(RegistryType.UI, Registry.Registry.TypeKey(GetType()), this);
    }
  }
}
