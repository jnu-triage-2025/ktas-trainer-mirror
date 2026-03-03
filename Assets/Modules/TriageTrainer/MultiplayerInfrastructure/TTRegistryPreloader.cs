using UnityEngine;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.ItemDefinitions;

namespace TriageTrainer.MultiplayerInfrastructure
{
  public class TTRegistryPreloader: MonoBehaviour
  {
    void Awake()
    {
      RegisterItems();
    }

    public void RegisterItems()
    {
      Registry.RegisterItemDefinition<Ambubag>("ambubag");
      Registry.RegisterItemDefinition<Cannula16g>("16g");
      Registry.RegisterItemDefinition<Cannula18g>("18g");
      Registry.RegisterItemDefinition<Cannula20g>("20g");
      Registry.RegisterItemDefinition<Cannula22g>("22g");
      Registry.RegisterItemDefinition<Cannula24g>("24g");
      Registry.RegisterItemDefinition<CentralLineSet>("central_line_set");
      Registry.RegisterItemDefinition<DefibPad>("defibpad");
      Registry.RegisterItemDefinition<ElasticBand>("elasticband");
      Registry.RegisterItemDefinition<Electrode>("electrode");
      Registry.RegisterItemDefinition<ElectrodeCable>("electrode_cable");
      Registry.RegisterItemDefinition<Epinephrine>("epi");
      Registry.RegisterItemDefinition<EtTube>("et_tube");
      Registry.RegisterItemDefinition<EtTubeReady>("et_tube_ready");
      Registry.RegisterItemDefinition<FacialMask>("facialmask");
      Registry.RegisterItemDefinition<Gauze>("gauze");
      Registry.RegisterItemDefinition<Glove>("glove");
      Registry.RegisterItemDefinition<IvSet>("iv_set");
      Registry.RegisterItemDefinition<LaryngoBlade>("laryngo_blade");
      Registry.RegisterItemDefinition<LaryngoHandle>("laryngo_handle");
      Registry.RegisterItemDefinition<Laryngoscope>("laryngoscope");
      Registry.RegisterItemDefinition<Norepinephrine>("norepi");
      Registry.RegisterItemDefinition<Ns1000ml>("ns_1000ml");
      Registry.RegisterItemDefinition<Ns20ml>("ns_20ml");
      Registry.RegisterItemDefinition<O2Line>("o2_line");
      Registry.RegisterItemDefinition<Penlight>("penlight");
      Registry.RegisterItemDefinition<Plaster>("plaster");
      Registry.RegisterItemDefinition<ReservoirBag>("reservoir_bag");
      Registry.RegisterItemDefinition<Scissors>("scissors");
      Registry.RegisterItemDefinition<Stylet>("stylet");
      Registry.RegisterItemDefinition<SuctionCath>("suction_cath");
      Registry.RegisterItemDefinition<SuctionLine>("suction_line");
      Registry.RegisterItemDefinition<Swab>("swab");
      Registry.RegisterItemDefinition<Syringe20cc>("syringe_20cc");
      Registry.RegisterItemDefinition<Syringe50cc>("syringe_50cc");
      Registry.RegisterItemDefinition<Syringe5cc>("syringe_5cc");
      Registry.RegisterItemDefinition<TransfusionSet>("transfusion_set");
      Registry.RegisterItemDefinition<VitalSet>("vital_set");
      Registry.RegisterItemDefinition<WallSuction>("wall_suction");
      Registry.RegisterItemDefinition<Yankauer>("yankauer");
    }
  }
}
