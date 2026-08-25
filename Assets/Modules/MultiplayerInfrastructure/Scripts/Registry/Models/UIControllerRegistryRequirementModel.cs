using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// UI 컨트롤러의 등록 요구 사항입니다.
  /// identifier를 비워두면 컨트롤러 타입의 FullName을 자동으로 키로 사용합니다.
  /// </summary>
  [Serializable]
  public struct UIControllerRegistryRequirement
  {
    public string identifier;
    public MonoBehaviour controllerRef;
  }
}
