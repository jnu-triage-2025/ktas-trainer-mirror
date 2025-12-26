using System;
using UnityEngine;

namespace TriageTrainer.Dialogue
{
  /// <summary>
  /// 트리거 존 진입 시 자동으로 대화를 시작합니다.
  /// </summary>
  public class DialogueTriggerZone : MonoBehaviour
  {
    #region Serialized Fields

    [Header("Dialogue Settings")]
    [SerializeField] private TextAsset _dialogueJson;

    [Header("Trigger Settings")]
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private float _triggerCooldown = 1f;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _disableAfterTrigger = false;

    #endregion

    #region Private Fields

    private DialogueSession _cachedSession;
    private bool _hasTriggered;
    private float _lastTriggerTime;

    #endregion

    #region Events

    public static event Action<DialogueSession> OnDialogueRequested;

    #endregion

    #region Properties

    public bool HasTriggered => _hasTriggered;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
      CacheDialogueSession();
    }

    private void OnTriggerEnter(Collider other)
    {
      TryTrigger(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      TryTrigger(other.gameObject);
    }

    #endregion

    #region Trigger Logic

    private void TryTrigger(GameObject other)
    {
      if (!other.CompareTag(_playerTag))
        return;

      if (_triggerOnce && _hasTriggered)
        return;

      if (Time.time - _lastTriggerTime < _triggerCooldown)
        return;

      if (_cachedSession == null)
      {
        Debug.LogWarning($"[DialogueTriggerZone] {gameObject.name}: No valid dialogue session");
        return;
      }

      ExecuteTrigger();
    }

    private void ExecuteTrigger()
    {
      _hasTriggered = true;
      _lastTriggerTime = Time.time;

      OnDialogueRequested?.Invoke(_cachedSession);

      if (_disableAfterTrigger)
      {
        gameObject.SetActive(false);
      }
    }

    #endregion

    #region Dialogue Session

    private void CacheDialogueSession()
    {
      if (_dialogueJson == null)
        return;

      _cachedSession = DialogueJsonLoader.Load(_dialogueJson);
    }

    public void SetDialogue(TextAsset dialogueJson)
    {
      _dialogueJson = dialogueJson;
      _cachedSession = null;
      CacheDialogueSession();
    }

    public void SetDialogue(DialogueSession session)
    {
      _cachedSession = session;
    }

    #endregion

    #region Public API

    public void ResetTrigger()
    {
      _hasTriggered = false;
      _lastTriggerTime = 0f;

      if (!gameObject.activeSelf)
      {
        gameObject.SetActive(true);
      }
    }

    public void TriggerManually()
    {
      if (_cachedSession == null)
      {
        CacheDialogueSession();
      }

      if (_cachedSession != null)
      {
        ExecuteTrigger();
      }
    }

    #endregion

#if UNITY_EDITOR
    private void Reset()
    {
      _triggerOnce = true;
      _triggerCooldown = 1f;
      _playerTag = "Player";
    }

    private void OnDrawGizmos()
    {
      var collider = GetComponent<Collider>();
      if (collider == null) return;

      Gizmos.color = _hasTriggered
          ? new Color(0.5f, 0.5f, 0.5f, 0.3f)
          : new Color(0f, 1f, 0.5f, 0.3f);

      if (collider is BoxCollider box)
      {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.DrawWireCube(box.center, box.size);
      }
      else if (collider is SphereCollider sphere)
      {
        Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
      }
    }
#endif
  }
}
