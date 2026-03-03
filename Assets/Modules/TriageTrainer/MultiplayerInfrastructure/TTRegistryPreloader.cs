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
      Registry.RegisterItemDefinition<Epinephrine>(Epinephrine.Identifier);
      Registry.RegisterItemDefinition<EtTube>(EtTube.Identifier);
      Registry.RegisterItemDefinition<EtTubeReady>(EtTubeReady.Identifier);
      Registry.RegisterItemDefinition<FacialMask>(FacialMask.Identifier);
      Registry.RegisterItemDefinition<Gauze>(Gauze.Identifier);
      Registry.RegisterItemDefinition<Glove>(Glove.Identifier);
      Registry.RegisterItemDefinition<IvSet>(IvSet.Identifier);
      Registry.RegisterItemDefinition<LaryngoBlade>(LaryngoBlade.Identifier);
      Registry.RegisterItemDefinition<LaryngoHandle>(LaryngoHandle.Identifier);
      Registry.RegisterItemDefinition<Laryngoscope>(Laryngoscope.Identifier);
      Registry.RegisterItemDefinition<Norepinephrine>(Norepinephrine.Identifier);
      Registry.RegisterItemDefinition<Ns1000ml>(Ns1000ml.Identifier);
      Registry.RegisterItemDefinition<Ns20ml>(Ns20ml.Identifier);
      Registry.RegisterItemDefinition<O2Line>(O2Line.Identifier);
      Registry.RegisterItemDefinition<Penlight>(Penlight.Identifier);
      Registry.RegisterItemDefinition<Plaster>(Plaster.Identifier);
      Registry.RegisterItemDefinition<ReservoirBag>(ReservoirBag.Identifier);
      Registry.RegisterItemDefinition<Scissors>(Scissors.Identifier);
      Registry.RegisterItemDefinition<Stylet>(Stylet.Identifier);
      Registry.RegisterItemDefinition<SuctionCath>(SuctionCath.Identifier);
      Registry.RegisterItemDefinition<SuctionLine>(SuctionLine.Identifier);
      Registry.RegisterItemDefinition<Swab>(Swab.Identifier);
      Registry.RegisterItemDefinition<Syringe20cc>(Syringe20cc.Identifier);
      Registry.RegisterItemDefinition<Syringe50cc>(Syringe50cc.Identifier);
      Registry.RegisterItemDefinition<Syringe5cc>(Syringe5cc.Identifier);
      Registry.RegisterItemDefinition<TransfusionSet>(TransfusionSet.Identifier);
      Registry.RegisterItemDefinition<VitalSet>(VitalSet.Identifier);
      Registry.RegisterItemDefinition<WallSuction>(WallSuction.Identifier);
      Registry.RegisterItemDefinition<Yankauer>(Yankauer.Identifier);
    }
  }
}
