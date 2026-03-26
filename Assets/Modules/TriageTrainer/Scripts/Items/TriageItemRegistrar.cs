using MultiplayerInfrastructure.Registry;
using TriageTrainer.ItemDefinitions;
using UnityEngine;

namespace TriageTrainer.Items
{
  /// <summary>
  /// TriageTrainer 아이템 전체를 Registry에 일괄 등록합니다.
  ///
  /// ■ 사용법 (게임 초기화 시 1회 호출):
  ///   TriageItemRegistrar.RegisterAll();
  ///
  /// ■ 이후 인스턴스 생성:
  ///   var item = Registry.Registry.CreateItemInstance("16g");
  /// </summary>
  public static class TriageItemRegistrar
  {
    private static bool _registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
      RegisterAll();
    }

    public static void RegisterAll()
    {
      if (_registered)
        return;

      _registered = true;

      // ── Cannula ────────────────────────────────────────────────────────
      Registry.RegisterItemDefinition<Cannula16g>(Cannula16g.Identifier);
      Registry.RegisterItemDefinition<Cannula18g>(Cannula18g.Identifier);
      Registry.RegisterItemDefinition<Cannula20g>(Cannula20g.Identifier);
      Registry.RegisterItemDefinition<Cannula22g>(Cannula22g.Identifier);
      Registry.RegisterItemDefinition<Cannula24g>(Cannula24g.Identifier);

      // ── Respiratory ────────────────────────────────────────────────────
      Registry.RegisterItemDefinition<Ambubag>(Ambubag.Identifier);
      Registry.RegisterItemDefinition<FacialMask>(FacialMask.Identifier);
      Registry.RegisterItemDefinition<EndotrachealTube>(EndotrachealTube.Identifier);
      Registry.RegisterItemDefinition<EndotrachealTubeReady>(EndotrachealTubeReady.Identifier);
      Registry.RegisterItemDefinition<O2Line>(O2Line.Identifier);
      Registry.RegisterItemDefinition<ReservoirBag>(ReservoirBag.Identifier);
      Registry.RegisterItemDefinition<SuctionCatheter>(SuctionCatheter.Identifier);
      Registry.RegisterItemDefinition<SuctionLine>(SuctionLine.Identifier);
      Registry.RegisterItemDefinition<WallSuction>(WallSuction.Identifier);
      Registry.RegisterItemDefinition<Yankauer>(Yankauer.Identifier);
      Registry.RegisterItemDefinition<Stylet>(Stylet.Identifier);

      // ── Vascular & Pharmacology ────────────────────────────────────────
      Registry.RegisterItemDefinition<IvSet>(IvSet.Identifier);
      Registry.RegisterItemDefinition<CentralLineSet>(CentralLineSet.Identifier);
      Registry.RegisterItemDefinition<TransfusionSet>(TransfusionSet.Identifier);
      Registry.RegisterItemDefinition<EpinephrineAmpule>(EpinephrineAmpule.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine>(Norepinephrine.Identifier);
      Registry.RegisterItemDefinition<NormalSaline20ml>(NormalSaline20ml.Identifier);
      Registry.RegisterItemDefinition<NormalSaline1000ml>(NormalSaline1000ml.Identifier);

      // ── Monitoring & Laryngoscopy ──────────────────────────────────────
      Registry.RegisterItemDefinition<Electrode>(Electrode.Identifier);
      Registry.RegisterItemDefinition<ElectrodeCable>(ElectrodeCable.Identifier);
      Registry.RegisterItemDefinition<DefibPad>(DefibPad.Identifier);
      Registry.RegisterItemDefinition<VitalSet>(VitalSet.Identifier);
      Registry.RegisterItemDefinition<LaryngoscopeBlade>(LaryngoscopeBlade.Identifier);
      Registry.RegisterItemDefinition<LaryngoscopeHandle>(LaryngoscopeHandle.Identifier);
      Registry.RegisterItemDefinition<Laryngoscope>(Laryngoscope.Identifier);

      // ── Consumables ────────────────────────────────────────────────────
      Registry.RegisterItemDefinition<Gauze>(Gauze.Identifier);
      Registry.RegisterItemDefinition<Glove>(Glove.Identifier);
      Registry.RegisterItemDefinition<Swab>(Swab.Identifier);
      Registry.RegisterItemDefinition<Plaster>(Plaster.Identifier);
      Registry.RegisterItemDefinition<ElasticBand>(ElasticBand.Identifier);
      Registry.RegisterItemDefinition<Scissors>(Scissors.Identifier);
      Registry.RegisterItemDefinition<Penlight>(Penlight.Identifier);
      Registry.RegisterItemDefinition<Syringe5cc>(Syringe5cc.Identifier);
      Registry.RegisterItemDefinition<Syringe20cc>(Syringe20cc.Identifier);
      Registry.RegisterItemDefinition<Syringe50cc>(Syringe50cc.Identifier);
    }
  }
}
