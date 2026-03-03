using UnityEngine;

namespace TriageTrainer.Items
{
  /// <summary>
  /// TriageTrainer 아이템 정의를 Registry에 등록하는 초기화 컴포넌트입니다.
  ///
  /// 씬의 적절한 GameObject (예: GameManager, RegistryPreloaderController 와 같은 오브젝트)에 부착합니다.
  /// RegistryPreloaderController 보다 먼저 또는 같은 실행 순서에 있어야 합니다.
  /// </summary>
  public class TriageItemInitializerController : MonoBehaviour
  {
    private void Awake()
    {
      TriageItemRegistrar.RegisterAll();
    }
  }
}
