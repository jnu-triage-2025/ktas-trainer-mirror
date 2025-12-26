using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct ItemRegistryRequirement
  {
    public ItemBaseModelSO itemDataModel;
    public GameObject itemPrefab;
  }
}

