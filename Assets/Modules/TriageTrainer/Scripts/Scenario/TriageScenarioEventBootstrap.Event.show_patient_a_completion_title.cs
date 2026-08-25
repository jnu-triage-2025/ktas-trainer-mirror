using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const float PatientACompletionTitleDurationSeconds = 5f;

    private void RegisterEvent_ShowPatientACompletionTitle()
    {
      Register("show_patient_a_completion_title", Event_ShowPatientACompletionTitle);
    }

    private IEnumerator Event_ShowPatientACompletionTitle()
    {
      const string title = "시나리오 종료";
      const string subtitle = "시나리오를 완료하였습니다.";
      var titleUi = Registry.Get<TitleUIController>(
        RegistryType.UI,
        Registry.TypeKey<TitleUIController>());
      if (titleUi != null)
        titleUi.ShowTitle(title, subtitle);
      else
        Debug.LogWarning("[TriageScenarioEventBootstrap] Scenario completion Title UI was not found.", this);

      // 시나리오 이벤트는 서버에서 실행된다. 로컬 UI 호출만 하면 원격 참여자는 완료 화면을 받지 못한다.
      if (Registry.TryGet<ChatService>(RegistryType.Service, Registry.TypeKey<ChatService>(), out var chat)
          && InstanceFinder.IsServerStarted)
      {
        var targets = new List<NetworkConnection>();
        var clients = InstanceFinder.ServerManager?.Clients;
        if (clients != null)
        {
          foreach (var client in clients.Values)
          {
            if (client != null && client.IsValid)
              targets.Add(client);
          }
        }

        if (!chat.TryDispatchTitle(targets, title, subtitle, out var error))
          Debug.LogWarning($"[TriageScenarioEventBootstrap] Scenario completion title dispatch failed: {error}", this);
      }
      yield return new WaitForSeconds(PatientACompletionTitleDurationSeconds);
    }
  }
}
