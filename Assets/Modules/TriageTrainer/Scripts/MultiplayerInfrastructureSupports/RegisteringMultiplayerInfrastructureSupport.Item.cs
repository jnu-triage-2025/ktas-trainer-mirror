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
      Registry.RegisterItemDefinition<CervicalCollar>(CervicalCollar.Identifier);
      Registry.RegisterItemDefinition<BloodBag>(BloodBag.Identifier);
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
      Registry.RegisterItemDefinition<PlasmaSolution1000ml>(PlasmaSolution1000ml.Identifier);
      Registry.RegisterItemDefinition<Level1RapidInfuser>(Level1RapidInfuser.Identifier);

      // 조합 산출물(준비 완료) 아이템 — 리소스는 재료 아이템을 복사해 사용
      Registry.RegisterItemDefinition<NormalSalineIntravenousReady>(NormalSalineIntravenousReady.Identifier);
      Registry.RegisterItemDefinition<PlasmaSolutionIntravenousReady>(PlasmaSolutionIntravenousReady.Identifier);
      Registry.RegisterItemDefinition<YankauerSuctionReady>(YankauerSuctionReady.Identifier);
      Registry.RegisterItemDefinition<Oxyflowmeter>(Oxyflowmeter.Identifier);
      Registry.RegisterItemDefinition<HumidifierSterileDistilledWaterBottle>(HumidifierSterileDistilledWaterBottle.Identifier);
      Registry.RegisterItemDefinition<O2Line>(O2Line.Identifier);
      Registry.RegisterItemDefinition<SterileDistilledWater>(SterileDistilledWater.Identifier);
      Registry.RegisterItemDefinition<Flowmeter>(Flowmeter.Identifier);
      Registry.RegisterItemDefinition<Humidifier>(Humidifier.Identifier);
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
      Registry.RegisterItemDefinition<PatientMonitor>(PatientMonitor.Identifier);
      Registry.RegisterItemDefinition<TPieceSet>(TPieceSet.Identifier);
      Registry.RegisterItemDefinition<VitalSet>(VitalSet.Identifier);

      // ===== 튜토리얼 아이템 =====
      Registry.RegisterItemDefinition<TutorialDeliveryPackage>(TutorialDeliveryPackage.Identifier);
      Registry.RegisterItemDefinition<TutorialDecoyOvernightYouth>(TutorialDecoyOvernightYouth.Identifier);
      Registry.RegisterItemDefinition<TutorialDecoy8909>(TutorialDecoy8909.Identifier);
      Registry.RegisterItemDefinition<TutorialDecoyKimGangsanMail>(TutorialDecoyKimGangsanMail.Identifier);

      // ===== 튜토리얼 시계 제작 아이템 =====
      Registry.RegisterItemDefinition<TinIngot>(TinIngot.Identifier);
      Registry.RegisterItemDefinition<SmallGear>(SmallGear.Identifier);
      Registry.RegisterItemDefinition<SmallChain>(SmallChain.Identifier);
      Registry.RegisterItemDefinition<HandyClock>(HandyClock.Identifier);
      Registry.RegisterItemDefinition<WallSuction>(WallSuction.Identifier);
      Registry.RegisterItemDefinition<Yankauer>(Yankauer.Identifier);

      // ===== 몸통(주사기) + 용액 조합 완제품 (카테터 없음, 9종) =====
      Registry.RegisterItemDefinition<Epinephrine5ccSyringe>(Epinephrine5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine20ccSyringe>(Epinephrine20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Epinephrine50ccSyringe>(Epinephrine50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine5ccSyringe>(Norepinephrine5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine20ccSyringe>(Norepinephrine20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine50ccSyringe>(Norepinephrine50ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline5ccSyringe>(NormalSaline5ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline20ccSyringe>(NormalSaline20ccSyringe.Identifier);
      Registry.RegisterItemDefinition<NormalSaline50ccSyringe>(NormalSaline50ccSyringe.Identifier);

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

      // 생리식염수 1L 1개 + 수액세트 1개 → 식염수 수액 세트 1개
      // (시나리오 구식 산출물명 ns1_ready)
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSalineIntravenousReady.Identifier)
          .Requires(NormalSaline1000ml.Identifier, 1)
          .Requires(IntravenousSet.Identifier, 1)
          .Produces(1));

      // 플라즈마 솔루션 1L 1개 + 수액세트 1개 → 혈장 수액 세트 1개
      // (시나리오 구식 산출물명 ps1_ready)
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(PlasmaSolutionIntravenousReady.Identifier)
          .Requires(PlasmaSolution1000ml.Identifier, 1)
          .Requires(IntravenousSet.Identifier, 1)
          .Produces(1));

      // 석션 라인 1개 + 양커 석션 팁 1개 → 준비된 양커 석션 1개
      // (시나리오 구식 산출물명 yankauer_ready)
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(YankauerSuctionReady.Identifier)
          .Requires(SuctionLine.Identifier, 1)
          .Requires(Yankauer.Identifier, 1)
          .Produces(1));

      // 습윤병 1개 + 멸균증류수 1개 → 멸균증류수가 담긴 습윤병 1개
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(HumidifierSterileDistilledWaterBottle.Identifier)
          .Requires(Humidifier.Identifier, 1)
          .Requires(SterileDistilledWater.Identifier, 1)
          .Produces(1));

      // 유량계 1개 + 멸균증류수가 담긴 습윤병 1개 → 멸균증류수가 담긴 습윤병이 연결된 산소 유량계 1개
      // (시나리오 구식 산출물명 oxyflowmeter)
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Oxyflowmeter.Identifier)
          .Requires(Flowmeter.Identifier, 1)
          .Requires(HumidifierSterileDistilledWaterBottle.Identifier, 1)
          .Produces(1));

      // ===== [관계 1] 용액 + *cc 주사기 → 용액이 든 *cc 주사기 (카테터 없음, 9종) =====

      // 에피네프린 앰플 1 + 5cc 주사기 1 → 에피네프린이 든 5cc 주사기 1
      // (구 epinephrine_syringe 를 명명 규칙에 맞춰 대체)
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine5ccSyringe.Identifier)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Produces(1));

      // 에피네프린 앰플 1 + 20cc 주사기 1 → 에피네프린이 든 20cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine20ccSyringe.Identifier)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Produces(1));

      // 에피네프린 앰플 1 + 50cc 주사기 1 → 에피네프린이 든 50cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Epinephrine50ccSyringe.Identifier)
          .Requires(EpinephrineAmpule.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Produces(1));

      // 노르에피네프린 앰플 1 + 5cc 주사기 1 → 노르에피네프린이 든 5cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine5ccSyringe.Identifier)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Produces(1));

      // 노르에피네프린 앰플 1 + 20cc 주사기 1 → 노르에피네프린이 든 20cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine20ccSyringe.Identifier)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Produces(1));

      // 노르에피네프린 앰플 1 + 50cc 주사기 1 → 노르에피네프린이 든 50cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(Norepinephrine50ccSyringe.Identifier)
          .Requires(NorepinephrineAmpule.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Produces(1));

      // 생리식염수 1 + 5cc 주사기 1 → 생리식염수가 든 5cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline5ccSyringe.Identifier)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Requires(Syringe5cc.Identifier, 1)
          .Produces(1));

      // 생리식염수 1 + 20cc 주사기 1 → 생리식염수가 든 20cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline20ccSyringe.Identifier)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Requires(Syringe20cc.Identifier, 1)
          .Produces(1));

      // 생리식염수 1 + 50cc 주사기 1 → 생리식염수가 든 50cc 주사기 1
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(NormalSaline50ccSyringe.Identifier)
          .Requires(NormalSaline20ml.Identifier, 1)
          .Requires(Syringe50cc.Identifier, 1)
          .Produces(1));

      // ===== [관계 2] 용액이 든 *cc 주사기 + *게이지 → 용액이 든 *g *cc 주사기 (45종) =====
      RegisterFilledSyringeWithGaugeRecipes();

      // ===== [관계 3] 용액 + *cc 주사기 + *게이지 → 용액이 든 *g *cc 주사기 (45종) =====

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

      // ===== 튜토리얼 시계 제작 레시피 =====

      // 주석 주괴 3개 + 소형 톱니 5개 + 소형 사슬 1개 → 시계 1개
      ItemCombineRecipeRegistry.Register(
        new ItemCombineRecipe(HandyClock.Identifier)
          .Requires(TinIngot.Identifier, 3)
          .Requires(SmallGear.Identifier, 5)
          .Requires(SmallChain.Identifier, 1)
          .Produces(1));
    }

    /// <summary>
    /// [관계 2] 용액이 든 *cc 주사기 + *게이지(카테터) → 용액이 든 *g *cc 주사기 (45종).
    ///
    /// 관계 1(용액+주사기)로 만든 "용액이 든 *cc 주사기"에 카테터를 결합하면,
    /// 관계 3(용액+주사기+게이지)과 동일한 최종 산출물(`{solution}_{g}_{cc}_syringe`)이 된다.
    /// 즉 최종 완제품은 아래 두 경로 중 어느 쪽으로도 조합 가능하다:
    ///   (경로 A, 관계 1→2) 용액+주사기 → 용액이 든 주사기, + 게이지 → 완제품
    ///   (경로 B, 관계 3)    용액+주사기+게이지 → 완제품 (한 번에)
    ///
    /// 재료·산출물 식별자가 규칙적이므로 (용액 3종 × 게이지 5종 × 용량 3종) 조합을 표로 순회해 등록한다.
    /// </summary>
    private static void RegisterFilledSyringeWithGaugeRecipes()
    {
      // (용액 접두사, 용액이 든 *cc 주사기 식별자[5cc,20cc,50cc])
      var solutions = new (string prefix, string[] filledSyringeByVolume)[]
      {
        ("epinephrine", new[]
        {
          Epinephrine5ccSyringe.Identifier,
          Epinephrine20ccSyringe.Identifier,
          Epinephrine50ccSyringe.Identifier,
        }),
        ("norepinephrine", new[]
        {
          Norepinephrine5ccSyringe.Identifier,
          Norepinephrine20ccSyringe.Identifier,
          Norepinephrine50ccSyringe.Identifier,
        }),
        ("normal_saline", new[]
        {
          NormalSaline5ccSyringe.Identifier,
          NormalSaline20ccSyringe.Identifier,
          NormalSaline50ccSyringe.Identifier,
        }),
      };

      // (게이지 라벨, 카테터 식별자)
      var gauges = new (string label, string cannulaIdentifier)[]
      {
        ("16g", Cannula16g.Identifier),
        ("18g", Cannula18g.Identifier),
        ("20g", Cannula20g.Identifier),
        ("22g", Cannula22g.Identifier),
        ("24g", Cannula24g.Identifier),
      };

      // 용량 라벨(관계 순회용). filledSyringeByVolume 인덱스와 1:1 대응.
      var volumeLabels = new[] { "5cc", "20cc", "50cc" };

      foreach (var solution in solutions)
      {
        for (int v = 0; v < volumeLabels.Length; v++)
        {
          string filledSyringe = solution.filledSyringeByVolume[v];
          string volume = volumeLabels[v];

          foreach (var gauge in gauges)
          {
            // 최종 산출물 식별자: {용액}_{게이지}_{용량}_syringe (관계 3과 동일)
            string output = $"{solution.prefix}_{gauge.label}_{volume}_syringe";

            ItemCombineRecipeRegistry.Register(
              new ItemCombineRecipe(output)
                .Requires(filledSyringe, 1)
                .Requires(gauge.cannulaIdentifier, 1)
                .Produces(1));
          }
        }
      }
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

      foreach (var entry in registeredItems)
      {
        string id = entry.Key;
        Type itemType = entry.Value;

        // ── 아이콘 스프라이트 ─────────────────────────────────────────────────
        string iconPath = $"{DefaultsItemRegistry.ItemTexturesPath}/{id}";
        var sprite = Resources.Load<Sprite>(iconPath);
        if (sprite == null &&
            !ItemMissingAssetSuppression.ShouldSuppressSpriteMissingWarning(itemType))
        {
          Debug.LogWarning(
            $"[MultiplayerInfrastructureRegisterSupport] 아이콘 스프라이트 누락 " +
            $"(identifier: '{id}', 경로: Resources/{iconPath})");
          missingIconCount++;
        }

        // ── 3D 모델 프리팹 ────────────────────────────────────────────────────
        string modelPath = $"{ModelRootPath}/{id}";
        var prefab = Resources.Load<GameObject>(modelPath);
        if (prefab == null &&
            !ItemMissingAssetSuppression.ShouldSuppressModelMissingWarning(itemType))
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
