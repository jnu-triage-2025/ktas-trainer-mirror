using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Server;
using MultiplayerInfrastructure.Session;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Server
{
  public sealed class DedicatedServerOptionsTests
  {
    [Test]
    public void ParseWithoutArgumentsUsesBuiltInDefaults()
    {
      var options = DedicatedServerOptions.Parse(new string[0]);

      Assert.That(options.IsExplicitlyRequested, Is.False);
      Assert.That(options.BindAddress, Is.EqualTo(DedicatedServerOptions.DefaultBindAddress));
      Assert.That(options.Port, Is.EqualTo(DefaultsSessionInformationModel.port));
      Assert.That(options.SessionName, Is.EqualTo(DedicatedServerOptions.DefaultSessionName));
      Assert.That(options.UseLanDiscovery, Is.True);
      Assert.That(options.StartScene, Is.EqualTo(DefaultsSceneControl.IngameSceneName));
      Assert.That(options.DatapackIds, Is.Empty);
      Assert.That(options.Warnings, Is.Empty);
    }

    [Test]
    public void SessionConfigurationProvidesDefaultsWhenArgumentsAreAbsent()
    {
      var configuration = new SessionConfiguration
      {
        port = 40000,
        sessionName = "Configured Session",
        datapacks = new[] { "pack-a", "pack-b" }
      };

      var options = DedicatedServerOptions.Parse(new[] { "-dedicatedServer" }, configuration);

      Assert.That(options.Port, Is.EqualTo((ushort)40000));
      Assert.That(options.SessionName, Is.EqualTo("Configured Session"));
      Assert.That(options.DatapackIds, Is.EqualTo(new[] { "pack-a", "pack-b" }));
    }

    [Test]
    public void DefaultDatapackFolderNameIsStable()
    {
      Assert.That(DedicatedServerRuntime.DefaultDatapackFolderName, Is.EqualTo("DataPacks"));
    }

    [Test]
    public void CommandLineArgumentsOverrideSessionConfiguration()
    {
      var configuration = new SessionConfiguration
      {
        port = 40000,
        sessionName = "Configured Session",
        datapacks = new[] { "pack-a" }
      };

      var options = DedicatedServerOptions.Parse(
        new[] { "-port", "41000", "-sessionName", "Overridden", "-datapacks", "pack-b,pack-c" },
        configuration);

      Assert.That(options.Port, Is.EqualTo((ushort)41000));
      Assert.That(options.SessionName, Is.EqualTo("Overridden"));
      Assert.That(options.DatapackIds, Is.EqualTo(new[] { "pack-b", "pack-c" }));
    }

    [Test]
    public void SwitchesAreRecognizedRegardlessOfPrefixAndCase()
    {
      var options = DedicatedServerOptions.Parse(new[] { "--Server", "--Port=42000", "-BindAddress=192.168.0.10" });

      Assert.That(options.IsExplicitlyRequested, Is.True);
      Assert.That(options.Port, Is.EqualTo((ushort)42000));
      Assert.That(options.BindAddress, Is.EqualTo("192.168.0.10"));
    }

    [Test]
    public void DuplicateDatapackIdentifiersAreCollapsed()
    {
      var options = DedicatedServerOptions.Parse(new[] { "-datapacks", "pack-a, pack-b ,pack-a" });

      Assert.That(options.DatapackIds, Is.EqualTo(new[] { "pack-a", "pack-b" }));
    }

    [Test]
    public void DatapackSelectionCanBeClearedExplicitly()
    {
      var configuration = new SessionConfiguration { datapacks = new[] { "pack-a" } };

      var options = DedicatedServerOptions.Parse(new[] { "-noDatapacks" }, configuration);

      Assert.That(options.DatapackIds, Is.Empty);
    }

    [Test]
    public void DatapackFolderCanBeOverridden()
    {
      Assert.That(DedicatedServerOptions.Parse(new string[0]).DatapacksPath, Is.Null);
      Assert.That(
        DedicatedServerOptions.Parse(new[] { "-datapacksPath", "/srv/ktas/DataPacks" }).DatapacksPath,
        Is.EqualTo("/srv/ktas/DataPacks"));
    }

    [Test]
    public void LanBroadcastCanBeDisabled()
    {
      Assert.That(DedicatedServerOptions.Parse(new[] { "-noLanBroadcast" }).UseLanDiscovery, Is.False);
      Assert.That(DedicatedServerOptions.Parse(new[] { "-lanBroadcast", "false" }).UseLanDiscovery, Is.False);
      Assert.That(DedicatedServerOptions.Parse(new[] { "-lanBroadcast", "off" }).UseLanDiscovery, Is.False);
      Assert.That(DedicatedServerOptions.Parse(new[] { "-lanBroadcast" }).UseLanDiscovery, Is.True);
    }

    [Test]
    public void InvalidPortKeepsTheFallbackAndReportsAWarning()
    {
      var options = DedicatedServerOptions.Parse(new[] { "-port", "not-a-port" });

      Assert.That(options.Port, Is.EqualTo(DefaultsSessionInformationModel.port));
      Assert.That(options.Warnings, Is.Not.Empty);
    }

    [Test]
    public void MissingValueDoesNotConsumeTheFollowingSwitch()
    {
      var options = DedicatedServerOptions.Parse(new[] { "-port", "-dedicatedServer" });

      Assert.That(options.Port, Is.EqualTo(DefaultsSessionInformationModel.port));
      Assert.That(options.IsExplicitlyRequested, Is.True);
      Assert.That(options.Warnings, Is.Not.Empty);
    }

    [Test]
    public void UnityOwnedArgumentsAreIgnored()
    {
      var options = DedicatedServerOptions.Parse(new[]
      {
        "ktas-trainer-server.exe", "-batchmode", "-nographics", "-logFile", "server.log", "-dedicatedServer"
      });

      Assert.That(options.IsExplicitlyRequested, Is.True);
      Assert.That(options.Port, Is.EqualTo(DefaultsSessionInformationModel.port));
    }

    [Test]
    public void DedicatedModeActivatesForServerBuildsOrExplicitRequests()
    {
      Assert.That(DedicatedServerRuntime.ShouldActivate(isServerBuild: true, explicitlyRequested: false), Is.True);
      Assert.That(DedicatedServerRuntime.ShouldActivate(isServerBuild: false, explicitlyRequested: true), Is.True);
      Assert.That(DedicatedServerRuntime.ShouldActivate(isServerBuild: false, explicitlyRequested: false), Is.False);
    }

    [Test]
    public void SessionInformationIsBuiltFromTheParsedOptions()
    {
      var options = DedicatedServerOptions.Parse(new[]
      {
        "-dedicatedServer", "-bindAddress", "0.0.0.0", "-port", "37891", "-sessionName", "Room 1"
      });

      var sessionInformation = DedicatedServerRuntime.CreateSessionInformation(options);

      Assert.That(sessionInformation.Address, Is.EqualTo("0.0.0.0"));
      Assert.That(sessionInformation.Port, Is.EqualTo((ushort)37891));
      Assert.That(sessionInformation.Name, Is.EqualTo("Room 1"));
    }
  }
}
