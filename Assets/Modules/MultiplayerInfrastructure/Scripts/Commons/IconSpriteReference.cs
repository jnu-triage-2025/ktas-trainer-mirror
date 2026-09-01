using System;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Commons
{
  [Serializable]
  public class IconSpriteReference
  {
    [SerializeField] private Sprite _sprite;
    [SerializeField] private string _iconRegistryIdentifier;
    [SerializeField] private IconSpriteDefinitions _iconDefinition = IconSpriteDefinitions.Undefined;

    public Sprite Sprite => _sprite;
    public string IconRegistryIdentifier => _iconRegistryIdentifier;
    public IconSpriteDefinitions IconDefinition => _iconDefinition;
    public bool IsExplicitNone => _iconDefinition == IconSpriteDefinitions.None;

    /// <summary>
    /// 명시적으로 아이콘이 지정되지 않았는지 여부.
    /// 이 경우 사용처에서 컨텍스트별 기본 아이콘(예: 시나리오 실행 아이콘)을 적용할 수 있습니다.
    /// 직접 스프라이트, 정의(enum), 레지스트리 식별자 중 어느 것도 지정되지 않았고,
    /// 명시적 None도 아닌 상태를 의미합니다.
    /// </summary>
    public bool IsUnspecified =>
      _sprite == null
      && _iconDefinition == IconSpriteDefinitions.Undefined
      && string.IsNullOrWhiteSpace(_iconRegistryIdentifier);

    public Sprite Resolve()
    {
      return Resolve(null);
    }

    /// <summary>
    /// 아이콘을 해석합니다. 명시적으로 아이콘이 지정되지 않았고(<see cref="IsUnspecified"/>)
    /// <paramref name="defaultRegistryIdentifier"/> 가 주어지면 해당 식별자의 아이콘을 기본값으로 사용합니다.
    /// 명시적 None(<see cref="IsExplicitNone"/>)인 경우에는 기본값을 적용하지 않고 null을 반환합니다.
    /// </summary>
    public Sprite Resolve(string defaultRegistryIdentifier)
    {
      if (_sprite != null)
        return _sprite;

      if (_iconDefinition == IconSpriteDefinitions.None)
        return null;

      if (_iconDefinition != IconSpriteDefinitions.Undefined)
      {
        var iconRegistryIdentifier = GetIconRegistryIdentifierFromSpriteDefinitions(_iconDefinition);
        if (!string.IsNullOrWhiteSpace(iconRegistryIdentifier))
        {
          return Registry.Registry.Get<Sprite>(RegistryType.IconSprite, iconRegistryIdentifier);
        }
      }

      if (!string.IsNullOrWhiteSpace(_iconRegistryIdentifier))
        return Registry.Registry.Get<Sprite>(RegistryType.IconSprite, _iconRegistryIdentifier);

      if (!string.IsNullOrWhiteSpace(defaultRegistryIdentifier))
        return Registry.Registry.Get<Sprite>(RegistryType.IconSprite, defaultRegistryIdentifier);

      return null;
    }

    private string GetIconRegistryIdentifierFromSpriteDefinitions(IconSpriteDefinitions definition)
    {
      return definition switch
      {
        IconSpriteDefinitions.None => null,
        IconSpriteDefinitions.NPCMessage => "message-circle",
        IconSpriteDefinitions.NPCMessageQuest => "message-circle", // TODO: 실제 퀘스트 아이콘으로 교체한다
        IconSpriteDefinitions.Undefined => null,
        _ => null
      };
    }

    public IconSpriteReference Clone()
    {
      return new IconSpriteReference
      {
        _sprite = _sprite,
        _iconRegistryIdentifier = _iconRegistryIdentifier,
        _iconDefinition = _iconDefinition
      };
    }
  }
}
