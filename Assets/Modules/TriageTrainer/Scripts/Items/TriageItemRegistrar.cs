using MultiplayerInfrastructure.Registry;
using TriageTrainer.ItemDefinitions;

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
    public static void RegisterAll()
    {
      // ── Cannula ────────────────────────────────────────────────────────
      Registry.RegisterItemDefinition<Cannula16g>("16g");
      Registry.RegisterItemDefinition<Cannula18g>("18g");
      Registry.RegisterItemDefinition<Cannula20g>("20g");
      Registry.RegisterItemDefinition<Cannula22g>("22g");
      Registry.RegisterItemDefinition<Cannula24g>("24g");

      // ── Respiratory ────────────────────────────────────────────────────
      Registry.RegisterItemDefinition<Ambubag>("ambubag");
      Registry.RegisterItemDefinition<FacialMask>("facialmask");
      Registry.RegisterItemDefinition<EtTube>("et_tube");
      Registry.RegisterItemDefinition<EtTubeReady>("et_tube_ready");
      Registry.RegisterItemDefinition<O2Line>("o2_line");
      Registry.RegisterItemDefinition<ReservoirBag>("reservoir_bag");
      Registry.RegisterItemDefinition<SuctionCath>("suction_cath");
      Registry.RegisterItemDefinition<SuctionLine>("suction_line");
      Registry.RegisterItemDefinition<WallSuction>("wall_suction");
      Registry.RegisterItemDefinition<Yankauer>("yankauer");
      Registry.RegisterItemDefinition<Stylet>("stylet");

      // ── Vascular & Pharmacology ────────────────────────────────────────
      Registry.RegisterItemDefinition<IvSet>("iv_set");
      Registry.RegisterItemDefinition<CentralLineSet>("central_line_set");
      Registry.RegisterItemDefinition<TransfusionSet>("transfusion_set");
      Registry.RegisterItemDefinition<Epinephrine>("epi");
      Registry.RegisterItemDefinition<Norepinephrine>("norepi");
      Registry.RegisterItemDefinition<Ns20ml>("ns_20ml");
      Registry.RegisterItemDefinition<Ns1000ml>("ns_1000ml");

      // ── Monitoring & Laryngoscopy ──────────────────────────────────────
      Registry.RegisterItemDefinition<Electrode>("electrode");
      Registry.RegisterItemDefinition<ElectrodeCable>("electrode_cable");
      Registry.RegisterItemDefinition<DefibPad>("defibpad");
      Registry.RegisterItemDefinition<VitalSet>("vital_set");
      Registry.RegisterItemDefinition<LaryngoBlade>("laryngo_blade");
      Registry.RegisterItemDefinition<LaryngoHandle>("laryngo_handle");
      Registry.RegisterItemDefinition<Laryngoscope>("laryngoscope");

      // ── Consumables ────────────────────────────────────────────────────
      Registry.RegisterItemDefinition<Gauze>("gauze");
      Registry.RegisterItemDefinition<Glove>("glove");
      Registry.RegisterItemDefinition<Swab>("swab");
      Registry.RegisterItemDefinition<Plaster>("plaster");
      Registry.RegisterItemDefinition<ElasticBand>("elasticband");
      Registry.RegisterItemDefinition<Scissors>("scissors");
      Registry.RegisterItemDefinition<Penlight>("penlight");
      Registry.RegisterItemDefinition<Syringe5cc>("syringe_5cc");
      Registry.RegisterItemDefinition<Syringe20cc>("syringe_20cc");
      Registry.RegisterItemDefinition<Syringe50cc>("syringe_50cc");
    }
  }
}
