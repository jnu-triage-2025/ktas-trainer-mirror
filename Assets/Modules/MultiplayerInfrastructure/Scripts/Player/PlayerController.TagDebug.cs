using System.Collections;
using System.Collections.Generic;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [SerializeField] private string _debugTagUserIdentifier;
    [SerializeField] private string _debugTagUserDisplayName;
    [SerializeField] private List<string> _debugTags = new();
    [SerializeField] private bool _debugHasUserIdentifier;

    private Coroutine _debugTagRefreshRoutine;

    private void OnEnable()
    {
      if (!Application.isPlaying)
      {
        return;
      }

      if (_debugTagRefreshRoutine == null)
      {
        _debugTagRefreshRoutine = StartCoroutine(DebugTagRefreshRoutine());
      }
    }

    private void OnDisable()
    {
      if (_debugTagRefreshRoutine != null)
      {
        StopCoroutine(_debugTagRefreshRoutine);
        _debugTagRefreshRoutine = null;
      }
    }

    private IEnumerator DebugTagRefreshRoutine()
    {
      var wait = new WaitForSecondsRealtime(0.25f);
      while (true)
      {
        RefreshDebugTags();
        yield return wait;
      }
    }

    private void RefreshDebugTags()
    {
      _debugTagUserIdentifier = UserIdentifier;
      _debugTagUserDisplayName = UserDisplayName;
      _debugHasUserIdentifier = !string.IsNullOrWhiteSpace(_debugTagUserIdentifier);

      _debugTags.Clear();
      if (!_debugHasUserIdentifier)
      {
        return;
      }

      var currentTags = PlayerTagService.GetTags(_debugTagUserIdentifier);
      for (int i = 0; i < currentTags.Count; i++)
      {
        _debugTags.Add(currentTags[i]);
      }
    }
  }
}
