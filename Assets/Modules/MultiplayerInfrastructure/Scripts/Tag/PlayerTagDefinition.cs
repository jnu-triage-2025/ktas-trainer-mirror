using System;

namespace MultiplayerInfrastructure.Tag
{
  /// <summary>
  /// 플레이어 태그 하나의 정의.
  /// 태그를 부여하거나 제거할 때 커맨드 권한(<c>tag</c>)이 필요한지 여부를 담는다.
  ///
  /// <para>
  /// <c>Resources/Tag/*.tags.json</c> 파일과 데이터팩의 <c>tagDefinitions</c> 항목에서
  /// JsonUtility 로 읽으므로 필드 이름이 그대로 JSON 키가 된다.
  /// <see cref="requiresPermission"/> 를 생략한 정의는 안전 측으로 "권한 필요"가 된다.
  /// </para>
  /// </summary>
  [Serializable]
  public sealed class PlayerTagDefinition
  {
    /// <summary>태그 식별자. <see cref="PlayerTagService"/> 에 저장되는 문자열과 대소문자까지 같아야 한다.</summary>
    public string identifier;

    /// <summary>
    /// true 면 이 태그를 추가/제거/변경할 때 <c>tag</c> 커맨드 권한이 필요하다.
    /// false 면 권한이 없는 참가자도 <c>/tag add</c>, <c>/tag remove</c>, <c>/tag change</c> 로 다룰 수 있다.
    /// JSON 에서 생략되면 true 다.
    /// </summary>
    public bool requiresPermission = true;

    /// <summary>사람이 읽는 설명. 동작에는 쓰이지 않는다.</summary>
    public string description;

    public PlayerTagDefinition()
    {
    }

    public PlayerTagDefinition(string identifier, bool requiresPermission, string description = null)
    {
      this.identifier = identifier;
      this.requiresPermission = requiresPermission;
      this.description = description;
    }
  }

  /// <summary>
  /// <c>Resources/Tag/*.tags.json</c> 파일의 최상위 형식.
  /// </summary>
  [Serializable]
  public sealed class PlayerTagDefinitionFile
  {
    public PlayerTagDefinition[] tags;
  }
}
