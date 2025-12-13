using System;
using UnityEngine;

namespace TriageTrainer.Registry
{
  [Serializable]
  public struct ItemRegistryRequirement
  {
    public ItemBaseModelSO itemDataModel;
    public GameObject itemPrefab;
  }
}

