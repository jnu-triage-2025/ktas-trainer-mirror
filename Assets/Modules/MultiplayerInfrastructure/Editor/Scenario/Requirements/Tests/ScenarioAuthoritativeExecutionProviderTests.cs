using System.Linq;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Scenario.Requirements;
using NUnit.Framework;
using UnityEditor;

namespace MultiplayerInfrastructure.Tests.Scenario.Requirements
{
  public sealed class ScenarioAuthoritativeExecutionProviderTests
  {
    private const string CatalogPath =
      "Assets/Modules/MultiplayerInfrastructure/ScriptableObjects/Scenario Server Authoritative Execution Requirements.asset";

    [Test]
    public void StaticCatalogDeclaresServerAuthoritativeExecutionService()
    {
      var catalog = AssetDatabase.LoadAssetAtPath<ScenarioRequirementStaticProviderCatalog>(CatalogPath);

      Assert.That(catalog, Is.Not.Null);
      var entry = catalog.GetStaticProviders().Single(value =>
        value.Key.Kind == ScenarioRequirementKind.Service
        && value.Key.Identifier == ScenarioNetworkRelay.AuthoritativeExecutionServiceIdentifier);
      Assert.That(entry.Authority, Is.EqualTo(ScenarioRequirementAuthority.Server));
      Assert.That(entry.ProviderIdentifier, Is.EqualTo(ScenarioNetworkRelay.AuthoritativeExecutionServiceIdentifier));
    }
  }
}
