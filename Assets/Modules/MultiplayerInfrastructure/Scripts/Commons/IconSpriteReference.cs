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

    public Sprite Resolve()
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

      if (string.IsNullOrWhiteSpace(_iconRegistryIdentifier))
        return null;

      return Registry.Registry.Get<Sprite>(RegistryType.IconSprite, _iconRegistryIdentifier);
    }

    private string GetIconRegistryIdentifierFromSpriteDefinitions(IconSpriteDefinitions definition)
    {
      return definition switch
      {
        IconSpriteDefinitions.None => null,
        IconSpriteDefinitions.NPCMessage => "message-circle",
        IconSpriteDefinitions.NPCMessageQuest => "message-circle", // TODO: Replace with actual quest icon
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
