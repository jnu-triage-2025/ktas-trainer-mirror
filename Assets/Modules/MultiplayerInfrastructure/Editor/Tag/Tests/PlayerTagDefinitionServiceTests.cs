using MultiplayerInfrastructure.Tag;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Editor.Tag.Tests
{
  /// <summary>
  /// 태그 정의 카탈로그의 권한 정책과 출처별 등록/해제 규칙을 검증한다.
  /// </summary>
  public sealed class PlayerTagDefinitionServiceTests
  {
    private const string Source = "test:definitions";

    [SetUp]
    public void SetUp() => PlayerTagDefinitionService.Clear();

    [TearDown]
    public void TearDown() => PlayerTagDefinitionService.Clear();

    [Test]
    public void UndefinedTagRequiresPermissionByDefault()
    {
      Assert.That(PlayerTagDefinitionService.IsDefined("zz_undefined_tag"), Is.False);
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_undefined_tag"), Is.True);
    }

    [Test]
    public void BlankTagAlwaysRequiresPermission()
    {
      PlayerTagDefinitionService.DefaultRequiresPermission = false;

      Assert.That(PlayerTagDefinitionService.RequiresPermission(null), Is.True);
      Assert.That(PlayerTagDefinitionService.RequiresPermission(string.Empty), Is.True);
      Assert.That(PlayerTagDefinitionService.RequiresPermission("   "), Is.True);
    }

    [Test]
    public void DefaultPolicyAppliesOnlyToUndefinedTags()
    {
      PlayerTagDefinitionService.Define("zz_gated", requiresPermission: true, Source);
      PlayerTagDefinitionService.DefaultRequiresPermission = false;

      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_undefined_tag"), Is.False);
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_gated"), Is.True);
    }

    [Test]
    public void DefinedPermissionFreeTagDoesNotRequirePermission()
    {
      Assert.That(PlayerTagDefinitionService.Define("zz_free", requiresPermission: false, Source), Is.True);

      Assert.That(PlayerTagDefinitionService.IsDefined("zz_free"), Is.True);
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_free"), Is.False);
      Assert.That(PlayerTagDefinitionService.TryGetDefinition("zz_free", out var definition), Is.True);
      Assert.That(definition.identifier, Is.EqualTo("zz_free"));
      Assert.That(definition.requiresPermission, Is.False);
    }

    [Test]
    public void TagIdentifiersAreCaseSensitiveLikeStoredTags()
    {
      PlayerTagDefinitionService.Define("zz_free", requiresPermission: false, Source);

      Assert.That(PlayerTagDefinitionService.RequiresPermission("ZZ_FREE"), Is.True);
    }

    [Test]
    public void LaterSourceWinsAndUndefineRestoresPreviousDefinition()
    {
      PlayerTagDefinitionService.Define("zz_role", requiresPermission: false, "source-a");
      PlayerTagDefinitionService.Define("zz_role", requiresPermission: true, "source-b");
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_role"), Is.True);

      Assert.That(PlayerTagDefinitionService.Undefine("source-b"), Is.EqualTo(1));
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_role"), Is.False);

      Assert.That(PlayerTagDefinitionService.Undefine("source-a"), Is.EqualTo(1));
      Assert.That(PlayerTagDefinitionService.IsDefined("zz_role"), Is.False);
      Assert.That(PlayerTagDefinitionService.Undefine("source-a"), Is.Zero);
    }

    [Test]
    public void RedefiningWithinTheSameSourceReplacesInPlace()
    {
      PlayerTagDefinitionService.Define("zz_role", requiresPermission: false, Source);
      PlayerTagDefinitionService.Define("zz_role", requiresPermission: true, Source);

      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_role"), Is.True);
      Assert.That(PlayerTagDefinitionService.Undefine(Source), Is.EqualTo(1));
      Assert.That(PlayerTagDefinitionService.IsDefined("zz_role"), Is.False);
    }

    [Test]
    public void DefineRejectsBlankIdentifiers()
    {
      Assert.That(PlayerTagDefinitionService.Define(null, Source), Is.False);
      Assert.That(PlayerTagDefinitionService.Define(new PlayerTagDefinition(), Source), Is.False);
      Assert.That(PlayerTagDefinitionService.Define("  ", requiresPermission: false, Source), Is.False);
    }

    [Test]
    public void JsonOmittingRequiresPermissionDefaultsToRequired()
    {
      const string json = "{\"tags\":["
        + "{\"identifier\":\"zz_omitted\"},"
        + "{\"identifier\":\"zz_free\",\"requiresPermission\":false},"
        + "{\"identifier\":\"zz_gated\",\"requiresPermission\":true},"
        + "{\"identifier\":\"\"}"
        + "]}";

      Assert.That(PlayerTagDefinitionService.TryLoadFromJson(json, Source, out int count, out string error), Is.True, error);
      Assert.That(count, Is.EqualTo(3));
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_omitted"), Is.True);
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_free"), Is.False);
      Assert.That(PlayerTagDefinitionService.RequiresPermission("zz_gated"), Is.True);
    }

    [Test]
    public void JsonWithoutTagsArrayOrWithInvalidSyntaxIsRejected()
    {
      Assert.That(PlayerTagDefinitionService.TryLoadFromJson("{}", Source, out _, out string missingError), Is.False);
      Assert.That(missingError, Is.Not.Empty);
      Assert.That(PlayerTagDefinitionService.TryLoadFromJson("not json", Source, out _, out string invalidError), Is.False);
      Assert.That(invalidError, Is.Not.Empty);
      Assert.That(PlayerTagDefinitionService.TryLoadFromJson(string.Empty, Source, out _, out string emptyError), Is.False);
      Assert.That(emptyError, Is.Not.Empty);
    }

    /// <summary>
    /// 간호사 역할 태그는 훈련생이 스스로 고르는 태그이므로 내장 정의 파일에서 권한이 필요 없게 선언되어야 한다.
    /// </summary>
    [TestCase("nurse_a")]
    [TestCase("nurse_b")]
    [TestCase("nurse_c")]
    [TestCase("nurse_d")]
    public void BuiltInCatalogDeclaresTriageNurseRolesPermissionFree(string tag)
    {
      Assert.That(PlayerTagDefinitionService.TryGetDefinition(tag, out var definition), Is.True,
        $"'{tag}' must be defined in Resources/Tag/*.tags.json.");
      Assert.That(definition.requiresPermission, Is.False);
      Assert.That(PlayerTagDefinitionService.RequiresPermission(tag), Is.False);
    }

    [Test]
    public void BuiltInDefinitionsLoseToLaterSourcesAndComeBackAfterUndefine()
    {
      PlayerTagDefinitionService.Define("nurse_a", requiresPermission: true, "datapack:test");
      Assert.That(PlayerTagDefinitionService.RequiresPermission("nurse_a"), Is.True);

      PlayerTagDefinitionService.Undefine("datapack:test");
      Assert.That(PlayerTagDefinitionService.RequiresPermission("nurse_a"), Is.False);
    }

    [Test]
    public void GetDefinitionsReturnsEffectiveDefinitionsSortedByIdentifier()
    {
      PlayerTagDefinitionService.Define("zz_b", requiresPermission: false, "source-a");
      PlayerTagDefinitionService.Define("zz_a", requiresPermission: true, "source-a");
      PlayerTagDefinitionService.Define("zz_a", requiresPermission: false, "source-b");

      var definitions = PlayerTagDefinitionService.GetDefinitions();
      int indexA = -1, indexB = -1;
      for (int i = 0; i < definitions.Count; i++)
      {
        if (definitions[i].identifier == "zz_a") indexA = i;
        if (definitions[i].identifier == "zz_b") indexB = i;
      }

      Assert.That(indexA, Is.GreaterThanOrEqualTo(0));
      Assert.That(indexB, Is.GreaterThan(indexA));
      Assert.That(definitions[indexA].requiresPermission, Is.False);
    }
  }
}
