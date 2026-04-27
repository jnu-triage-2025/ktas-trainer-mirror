using FishNet.Object;
using UnityEngine;
namespace MultiplayerInfrastructure.Player
{
  public class PlayerCharacterModelAttachPoint : MonoBehaviour
  {
    public void ClearAttachedModel()
    {
      ClearChildren();
    }

    public GameObject ReplaceAttachedModel(GameObject characterModelObject)
    {
      if (characterModelObject == null)
      {
        Debug.LogWarning("[PlayerCharacterModelAttachPoint] Cannot replace player model with null object.", this);
        return null;
      }

      if (!characterModelObject.TryGetComponent<IPlayerCharacterModelObject>(out _))
      {
        Debug.LogWarning($"[PlayerCharacterModelAttachPoint] '{characterModelObject.name}' does not implement IPlayerCharacterModelObject.", this);
        return null;
      }

      ClearChildren();

      var modelInstance = Instantiate(characterModelObject, transform);
      StripNetworkComponents(modelInstance);
      modelInstance.transform.localPosition = Vector3.zero;
      modelInstance.transform.localRotation = Quaternion.identity;
      modelInstance.transform.localScale = Vector3.one;
      return modelInstance;
    }

    public GameObject ReplaceAttachedModel(IPlayerCharacterModelObject characterModelObject)
    {
      if (characterModelObject is not Component modelComponent)
      {
        Debug.LogWarning("[PlayerCharacterModelAttachPoint] IPlayerCharacterModelObject must be a Component.", this);
        return null;
      }

      return ReplaceAttachedModel(modelComponent.gameObject);
    }

    private void ClearChildren()
    {
      for (int i = transform.childCount - 1; i >= 0; i--)
      {
        var child = transform.GetChild(i);
        if (child == null)
          continue;

        var childGameObject = child.gameObject;
        StripNetworkComponents(childGameObject);
        childGameObject.SetActive(false);

        if (Application.isPlaying)
          Destroy(childGameObject);
        else
          DestroyImmediate(childGameObject);
      }
    }

    private static void StripNetworkComponents(GameObject target)
    {
      if (target == null)
        return;

      var networkBehaviours = target.GetComponentsInChildren<NetworkBehaviour>(true);
      foreach (var networkBehaviour in networkBehaviours)
      {
        if (networkBehaviour != null)
          Destroy(networkBehaviour);
      }

      var networkObjects = target.GetComponentsInChildren<NetworkObject>(true);
      foreach (var networkObject in networkObjects)
      {
        if (networkObject != null)
          Destroy(networkObject);
      }
    }
  }
}
