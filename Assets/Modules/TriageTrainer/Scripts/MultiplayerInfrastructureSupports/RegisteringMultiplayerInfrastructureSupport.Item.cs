/// TTRegistryMonoBehaviourSupport는 TriageTrainer 범위의 구현물에 대해서,
/// MultiplayerInfrastructure에 구현물 정의를 등록하기 위해 개별 구현되었습니다.

using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.ItemSystem;
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
      RegisterAllCombineRecipes();
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
      Registry.RegisterItemDefinition<EpinephrineSyringe>(EpinephrineSyringe.Identifier);
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
      Registry.RegisterItemDefinition<PlasmaSolution1000ml>(PlasmaSolution1000ml.Identifier);
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

      // ===== 바늘(게이지) + 몸통(주사기) + 용액 조합 완제품 (45종) =====
      Registry.RegisterItemDefinition<Epinephrine16g5ccSyringe>(Epinephrine16g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine16g20ccSyringe>(Epinephrine16g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine16g50ccSyringe>(Epinephrine16g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine18g5ccSyringe>(Epinephrine18g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine18g20ccSyringe>(Epinephrine18g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine18g50ccSyringe>(Epinephrine18g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine20g5ccSyringe>(Epinephrine20g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine20g20ccSyringe>(Epinephrine20g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine20g50ccSyringe>(Epinephrine20g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine22g5ccSyringe>(Epinephrine22g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine22g20ccSyringe>(Epinephrine22g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine22g50ccSyringe>(Epinephrine22g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine24g5ccSyringe>(Epinephrine24g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine24g20ccSyringe>(Epinephrine24g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine24g50ccSyringe>(Epinephrine24g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine16g5ccSyringe>(Norepinephrine16g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine16g20ccSyringe>(Norepinephrine16g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine16g50ccSyringe>(Norepinephrine16g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine18g5ccSyringe>(Norepinephrine18g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine18g20ccSyringe>(Norepinephrine18g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine18g50ccSyringe>(Norepinephrine18g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine20g5ccSyringe>(Norepinephrine20g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine20g20ccSyringe>(Norepinephrine20g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine20g50ccSyringe>(Norepinephrine20g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine22g5ccSyringe>(Norepinephrine22g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine22g20ccSyringe>(Norepinephrine22g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine22g50ccSyringe>(Norepinephrine22g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine24g5ccSyringe>(Norepinephrine24g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine24g20ccSyringe>(Norepinephrine24g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine24g50ccSyringe>(Norepinephrine24g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline16g5ccSyringe>(NormalSaline16g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline16g20ccSyringe>(NormalSaline16g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline16g50ccSyringe>(NormalSaline16g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline18g5ccSyringe>(NormalSaline18g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline18g20ccSyringe>(NormalSaline18g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline18g50ccSyringe>(NormalSaline18g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline20g5ccSyringe>(NormalSaline20g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline20g20ccSyringe>(NormalSaline20g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline20g50ccSyringe>(NormalSaline20g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline22g5ccSyringe>(NormalSaline22g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline22g20ccSyringe>(NormalSaline22g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline22g50ccSyringe>(NormalSaline22g50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline24g5ccSyringe>(NormalSaline24g5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline24g20ccSyringe>(NormalSaline24g20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline24g50ccSyringe>(NormalSaline24g50ccSyringe.Identifier);
    }

    // =========================================================================
    // 자동 조합 레시피 등록
    // =========================================================================

    /// <summary>
    /// 아이템 자동 조합 레시피를 등록합니다.
    ///
    /// 레시피 형식:
    ///   new ItemCombineRecipe(결과_식별자)
    ///     .Requires(재료_식별자, 필요_수량)
    ///     ...
    ///     .Produces(생성_수량);
    ///
    /// 새 조합 규칙 추가 시 이 메서드에만 등록하면 됩니다.
    /// </summary>
    public static void RegisterAllCombineRecipes()
    {
      ItemCombineRecipeRegistry.Clear();

      // 후두경 블레이드 1개 + 후두경 손잡이 1개 → 후두경 1개
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Laryngoscope.Identifier)
          .Requires(LaryngoscopeBlade.Identifier, 1)
          .Requires(LaryngoscopeHandle.Identifier, 1)
          .Produces(1));

      // 기관내관 1개 + 스타일렛 1개 → 기관내관 (준비 완료) 1개
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(EndotrachealTubeReady.Identifier)
          .Requires(EndotrachealTube.Identifier, 1)
          .Requires(Stylet.Identifier, 1)
          .Produces(1));

      // 5cc 주사기 1개 + 에피네프린 앰플 1개 → 에피네프린 주사기 1개
      // (주사기에 에피네프린 1mg 을 준비)
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(EpinephrineSyringe.Identifier)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // ===== 바늘(게이지) + 몸통(주사기) + 용액 조합 레시피 (45종) =====

      // 에피네프린이 든 16g 5cc 주사기 = 16g 카테터 1 + 5cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine16g5ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 16g 20cc 주사기 = 16g 카테터 1 + 20cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine16g20ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 16g 50cc 주사기 = 16g 카테터 1 + 50cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine16g50ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 18g 5cc 주사기 = 18g 카테터 1 + 5cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine18g5ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 18g 20cc 주사기 = 18g 카테터 1 + 20cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine18g20ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 18g 50cc 주사기 = 18g 카테터 1 + 50cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine18g50ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 20g 5cc 주사기 = 20g 카테터 1 + 5cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine20g5ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 20g 20cc 주사기 = 20g 카테터 1 + 20cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine20g20ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 20g 50cc 주사기 = 20g 카테터 1 + 50cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine20g50ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 22g 5cc 주사기 = 22g 카테터 1 + 5cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine22g5ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 22g 20cc 주사기 = 22g 카테터 1 + 20cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine22g20ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 22g 50cc 주사기 = 22g 카테터 1 + 50cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine22g50ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 24g 5cc 주사기 = 24g 카테터 1 + 5cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine24g5ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 24g 20cc 주사기 = 24g 카테터 1 + 20cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine24g20ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 에피네프린이 든 24g 50cc 주사기 = 24g 카테터 1 + 50cc 주사기 1 + 에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine24g50ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 16g 5cc 주사기 = 16g 카테터 1 + 5cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine16g5ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 16g 20cc 주사기 = 16g 카테터 1 + 20cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine16g20ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 16g 50cc 주사기 = 16g 카테터 1 + 50cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine16g50ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 18g 5cc 주사기 = 18g 카테터 1 + 5cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine18g5ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 18g 20cc 주사기 = 18g 카테터 1 + 20cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine18g20ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 18g 50cc 주사기 = 18g 카테터 1 + 50cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine18g50ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 20g 5cc 주사기 = 20g 카테터 1 + 5cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine20g5ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 20g 20cc 주사기 = 20g 카테터 1 + 20cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine20g20ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 20g 50cc 주사기 = 20g 카테터 1 + 50cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine20g50ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 22g 5cc 주사기 = 22g 카테터 1 + 5cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine22g5ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 22g 20cc 주사기 = 22g 카테터 1 + 20cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine22g20ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 22g 50cc 주사기 = 22g 카테터 1 + 50cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine22g50ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 24g 5cc 주사기 = 24g 카테터 1 + 5cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine24g5ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 24g 20cc 주사기 = 24g 카테터 1 + 20cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine24g20ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 노르에피네프린이 든 24g 50cc 주사기 = 24g 카테터 1 + 50cc 주사기 1 + 노르에피네프린 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine24g50ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 16g 5cc 주사기 = 16g 카테터 1 + 5cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline16g5ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 16g 20cc 주사기 = 16g 카테터 1 + 20cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline16g20ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 16g 50cc 주사기 = 16g 카테터 1 + 50cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline16g50ccSyringe.Identifier)
          .Requires(Cannula16g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 18g 5cc 주사기 = 18g 카테터 1 + 5cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline18g5ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 18g 20cc 주사기 = 18g 카테터 1 + 20cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline18g20ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 18g 50cc 주사기 = 18g 카테터 1 + 50cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline18g50ccSyringe.Identifier)
          .Requires(Cannula18g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 20g 5cc 주사기 = 20g 카테터 1 + 5cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline20g5ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 20g 20cc 주사기 = 20g 카테터 1 + 20cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline20g20ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 20g 50cc 주사기 = 20g 카테터 1 + 50cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline20g50ccSyringe.Identifier)
          .Requires(Cannula20g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 22g 5cc 주사기 = 22g 카테터 1 + 5cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline22g5ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 22g 20cc 주사기 = 22g 카테터 1 + 20cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline22g20ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 22g 50cc 주사기 = 22g 카테터 1 + 50cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline22g50ccSyringe.Identifier)
          .Requires(Cannula22g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 24g 5cc 주사기 = 24g 카테터 1 + 5cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline24g5ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 24g 20cc 주사기 = 24g 카테터 1 + 20cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline24g20ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));

      // 생리식염수가 든 24g 50cc 주사기 = 24g 카테터 1 + 50cc 주사기 1 + 생리식염수 앰플 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline24g50ccSyringe.Identifier)
          .Requires(Cannula24g.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Produces(1));
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
