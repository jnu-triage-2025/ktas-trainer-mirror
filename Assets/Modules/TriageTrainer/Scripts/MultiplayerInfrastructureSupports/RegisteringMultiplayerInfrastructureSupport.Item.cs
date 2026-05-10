/// TTRegistryMonoBehaviourSupport는 TriageTrainer 범위의 구현물에 대해서,
/// MultiplayerInfrastructure에 구현물 정의를 등록하기 위해 개별 구현되었습니다.

using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.ItemDefinitions;
using UnityEngine;
using MIExamples = MultiplayerInfrastructure.ItemSystem.Examples;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  /// <summary>
  /// MultiplayerInfrastructure 레지스트리 연동 지원 MonoBehaviour입니다.
  ///
  /// Awake() 에서 아이템 정의를 Registry에 등록하고,
  /// 등록된 모든 아이템에 대해 필요한 리소스(아이콘 스프라이트, 3D 모델 프리팹)가
  /// Resources 폴더 내에 존재하는지 검증합니다.
  /// </summary>
  public partial class RegisteringMultiplayerInfrastructureSupport : MonoBehaviour
  {
    // 3D 모델 프리팹이 위치한 Resources 하위 경로 (ItemObject.ModelRootPath 와 동일)
    private const string ModelRootPath = "Models/Items";

    private void Awake_Item()
    {
      RegisterAllItems();
      ValidateItemResources();
    }

    // =========================================================================
    // 아이템 등록
    // =========================================================================

    public static void RegisterAllItems()
    {
      Registry.RegisterItemDefinition<MIExamples.StoneBlock>(MIExamples.StoneBlock.Identifier);
      Registry.RegisterItemDefinition<MIExamples.WoodBlock>(MIExamples.WoodBlock.Identifier);
      Registry.RegisterItemDefinition<Ambubag>(Ambubag.Identifier);
      Registry.RegisterItemDefinition<Cannula16g>(Cannula16g.Identifier);
      Registry.RegisterItemDefinition<Cannula18g>(Cannula18g.Identifier);
      Registry.RegisterItemDefinition<Cannula20g>(Cannula20g.Identifier);
      Registry.RegisterItemDefinition<Cannula22g>(Cannula22g.Identifier);
      Registry.RegisterItemDefinition<Cannula24g>(Cannula24g.Identifier);
      Registry.RegisterItemDefinition<CentralLineSet>(CentralLineSet.Identifier);
      Registry.RegisterItemDefinition<DefibPad>(DefibPad.Identifier);
      Registry.RegisterItemDefinition<ElasticBand>(ElasticBand.Identifier);
      Registry.RegisterItemDefinition<Electrode>(Electrode.Identifier);
      Registry.RegisterItemDefinition<ElectrodeCable>(ElectrodeCable.Identifier);
      Registry.RegisterItemDefinition<EpinephrineAmpule>(EpinephrineAmpule.Identifier);
      Registry.RegisterItemDefinition<EndotrachealTube>(EndotrachealTube.Identifier);
      Registry.RegisterItemDefinition<EndotrachealTubeReady>(EndotrachealTubeReady.Identifier);
      Registry.RegisterItemDefinition<FacialMask>(FacialMask.Identifier);
      Registry.RegisterItemDefinition<Gauze>(Gauze.Identifier);
      Registry.RegisterItemDefinition<Gloves>(Gloves.Identifier);
      Registry.RegisterItemDefinition<IntravenousSet>(IntravenousSet.Identifier);
      Registry.RegisterItemDefinition<LaryngoscopeBlade>(LaryngoscopeBlade.Identifier);
      Registry.RegisterItemDefinition<LaryngoscopeHandle>(LaryngoscopeHandle.Identifier);
      Registry.RegisterItemDefinition<Laryngoscope>(Laryngoscope.Identifier);
      Registry.RegisterItemDefinition<NorepinephrineAmpule>(NorepinephrineAmpule.Identifier);
      Registry.RegisterItemDefinition<NormalSaline1000ml>(NormalSaline1000ml.Identifier);
      Registry.RegisterItemDefinition<NormalSaline20ml>(NormalSaline20ml.Identifier);
      Registry.RegisterItemDefinition<O2Line>(O2Line.Identifier);
      Registry.RegisterItemDefinition<Penlight>(Penlight.Identifier);
      Registry.RegisterItemDefinition<Plaster>(Plaster.Identifier);
      Registry.RegisterItemDefinition<ReservoirBag>(ReservoirBag.Identifier);
      Registry.RegisterItemDefinition<Scissors>(Scissors.Identifier);
      Registry.RegisterItemDefinition<Stylet>(Stylet.Identifier);
      Registry.RegisterItemDefinition<SuctionCatheter>(SuctionCatheter.Identifier);
      Registry.RegisterItemDefinition<SuctionLine>(SuctionLine.Identifier);
      Registry.RegisterItemDefinition<Swab>(Swab.Identifier);
      Registry.RegisterItemDefinition<Syringe20cc>(Syringe20cc.Identifier);
      Registry.RegisterItemDefinition<Syringe50cc>(Syringe50cc.Identifier);
      Registry.RegisterItemDefinition<Syringe5cc>(Syringe5cc.Identifier);
      Registry.RegisterItemDefinition<BloodTransfusionSet>(BloodTransfusionSet.Identifier);
      Registry.RegisterItemDefinition<VitalSet>(VitalSet.Identifier);
      Registry.RegisterItemDefinition<WallSuction>(WallSuction.Identifier);
      Registry.RegisterItemDefinition<Yankauer>(Yankauer.Identifier);
    }

    // =========================================================================
    // 리소스 검증
    // =========================================================================

    /// <summary>
    /// Registry에 등록된 모든 아이템 식별자에 대해 필요한 리소스가
    /// Resources 폴더 내에 존재하는지 확인합니다.
    ///
    /// 검증 항목:
    ///   • 아이콘 스프라이트 : Resources/{DefaultsItemRegistry.ItemTexturesPath}/{id}
    ///   • 3D 모델 프리팹   : Resources/Models/Items/{id}
    ///
    /// 누락된 리소스는 Debug.LogWarning 으로 출력됩니다.
    /// </summary>
    public void ValidateItemResources()
    {
      IReadOnlyDictionary<string, Type> registeredItems =
        Registry.GetAll<Type>(RegistryType.Item);

      if (registeredItems.Count == 0)
      {
        Debug.LogWarning("[MultiplayerInfrastructureRegisterSupport] 등록된 아이템이 없습니다.");
        return;
      }

      int missingIconCount = 0;
      int missingModelCount = 0;

      foreach (string id in registeredItems.Keys)
      {
        // ── 아이콘 스프라이트 ─────────────────────────────────────────────────
        string iconPath = $"{DefaultsItemRegistry.ItemTexturesPath}/{id}";
        var sprite = Resources.Load<Sprite>(iconPath);
        if (sprite == null)
        {
          Debug.LogWarning(
            $"[MultiplayerInfrastructureRegisterSupport] 아이콘 스프라이트 누락 " +
            $"(identifier: '{id}', 경로: Resources/{iconPath})");
          missingIconCount++;
        }

        // ── 3D 모델 프리팹 ────────────────────────────────────────────────────
        string modelPath = $"{ModelRootPath}/{id}";
        var prefab = Resources.Load<GameObject>(modelPath);
        if (prefab == null)
        {
          Debug.LogWarning(
            $"[MultiplayerInfrastructureRegisterSupport] 3D 모델 프리팹 누락 " +
            $"(identifier: '{id}', 경로: Resources/{modelPath})");
          missingModelCount++;
        }
      }

      // ── 검증 요약 ─────────────────────────────────────────────────────────
      if (missingIconCount == 0 && missingModelCount == 0)
      {
        Debug.Log(
          $"[MultiplayerInfrastructureRegisterSupport] 아이템 리소스 검증 완료 — " +
          $"등록된 아이템 {registeredItems.Count}개 모두 정상.");
      }
      else
      {
        Debug.LogWarning(
          $"[MultiplayerInfrastructureRegisterSupport] 아이템 리소스 검증 완료 — " +
          $"총 {registeredItems.Count}개 아이템 중 " +
          $"아이콘 {missingIconCount}개 누락, " +
          $"모델 {missingModelCount}개 누락.");
      }
    }
  }
}
