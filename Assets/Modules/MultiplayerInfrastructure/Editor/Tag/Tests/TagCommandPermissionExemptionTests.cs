using System.Reflection;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Permission;
using MultiplayerInfrastructure.Tag;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Tag.Tests
{
  /// <summary>
  /// 권한이 필요 없다고 정의된 태그는 'tag' 권한이 없어도 /tag add|remove|change 로 다룰 수 있어야 하고,
  /// 그 외의 태그와 서브커맨드는 여전히 권한을 요구해야 한다. 데이터팩 별칭을 거쳐도 같은 판단이 적용된다.
  /// </summary>
  public sealed class TagCommandPermissionExemptionTests
  {
    private const string Source = "test:exemption";
    private const string FreeTag = "zz_free_role";
    private const string GatedTag = "zz_gated_role";
    private const string UndefinedTag = "zz_undefined_role";

    // role 매핑이 없는 식별자는 기본 role(user) 을 받는다. 기본 role 은 'tag' 권한을 갖지 않는다.
    private const string UnprivilegedUser = "00000000-0000-4000-8000-tagexemption";

    private static readonly FieldInfo PermissionFileField = typeof(PermissionService)
      .GetField("_file", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo PermissionLoadedField = typeof(PermissionService)
      .GetField("_loaded", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo BuildDefaultPermissionFile = typeof(PermissionService)
      .GetMethod("BuildDefaultFile", BindingFlags.Static | BindingFlags.NonPublic);

    private object _savedPermissionFile;
    private bool _savedPermissionLoaded;

    [SetUp]
    public void SetUp()
    {
      PlayerTagDefinitionService.Clear();
      PlayerTagDefinitionService.Define(FreeTag, requiresPermission: false, Source);
      PlayerTagDefinitionService.Define(GatedTag, requiresPermission: true, Source);

      // 개발 머신의 permissions.json 에 영향을 받지 않도록 코드 기본값을 메모리에만 적용한다.
      Assert.That(PermissionFileField, Is.Not.Null);
      Assert.That(PermissionLoadedField, Is.Not.Null);
      Assert.That(BuildDefaultPermissionFile, Is.Not.Null);
      _savedPermissionFile = PermissionFileField.GetValue(null);
      _savedPermissionLoaded = (bool)PermissionLoadedField.GetValue(null);
      PermissionFileField.SetValue(null, BuildDefaultPermissionFile.Invoke(null, null));
      PermissionLoadedField.SetValue(null, true);
    }

    [TearDown]
    public void TearDown()
    {
      PermissionFileField.SetValue(null, _savedPermissionFile);
      PermissionLoadedField.SetValue(null, _savedPermissionLoaded);
      PlayerTagDefinitionService.Clear();
    }

    private static CommandDefinition_Tag CreateTagCommand() => new CommandDefinition_Tag(null);

    [Test]
    public void AddAndRemoveOfPermissionFreeTagAreExempt()
    {
      var command = CreateTagCommand();

      Assert.That(command.IsExemptFromPermission(null, new[] { "add", "@self", FreeTag }), Is.True);
      Assert.That(command.IsExemptFromPermission(null, new[] { "ADD", "name:Bob", FreeTag }), Is.True);
      Assert.That(command.IsExemptFromPermission(null, new[] { "remove", "@self", FreeTag }), Is.True);
    }

    [Test]
    public void AddOrRemoveOfGatedOrUndefinedTagIsNotExempt()
    {
      var command = CreateTagCommand();

      Assert.That(command.IsExemptFromPermission(null, new[] { "add", "@self", GatedTag }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "add", "@self", UndefinedTag }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "remove", "@self", GatedTag }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "remove", "@self", UndefinedTag }), Is.False);
    }

    [Test]
    public void ChangeIsExemptOnlyWhenBothTagsArePermissionFree()
    {
      PlayerTagDefinitionService.Define("zz_other_free", requiresPermission: false, Source);
      var command = CreateTagCommand();

      Assert.That(command.IsExemptFromPermission(null, new[] { "change", "@self", FreeTag, "zz_other_free" }), Is.True);
      Assert.That(command.IsExemptFromPermission(null, new[] { "change", "@self", FreeTag, "zz_other_free", "--force" }), Is.True);
      Assert.That(command.IsExemptFromPermission(null, new[] { "change", "@self", FreeTag, GatedTag }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "change", "@self", GatedTag, FreeTag }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "change", "@self", FreeTag, UndefinedTag }), Is.False);
    }

    [Test]
    public void ShowHelpAndMalformedArgumentsAreNotExempt()
    {
      var command = CreateTagCommand();

      Assert.That(command.IsExemptFromPermission(null, null), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new string[0]), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "show", "@self" }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "-h" }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "add" }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "add", "@self" }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "change", "@self", FreeTag }), Is.False);
      Assert.That(command.IsExemptFromPermission(null, new[] { "unknown", "@self", FreeTag }), Is.False);
    }

    /// <summary>
    /// 실행 경로는 세 번째 인자부터를 공백으로 이어 붙여 태그 이름으로 쓴다. 면제 판단도 같은 이름을 봐야 한다.
    /// </summary>
    [Test]
    public void ExemptionUsesTheSameJoinedTagNameAsExecution()
    {
      var command = CreateTagCommand();
      Assert.That(command.IsExemptFromPermission(null, new[] { "add", "@self", "zz", "spaced" }), Is.False);

      PlayerTagDefinitionService.Define("zz spaced", requiresPermission: false, Source);
      Assert.That(command.IsExemptFromPermission(null, new[] { "add", "@self", "zz", "spaced" }), Is.True);
    }

    [Test]
    public void CommandServicePermitsUnprivilegedUserOnlyForPermissionFreeTags()
    {
      var gameObject = new GameObject("ChatCommandService Test");
      try
      {
        var service = gameObject.AddComponent<ChatCommandService>();
        service.Initialize(null);

        Assert.That(PermissionService.HasPermission(UnprivilegedUser, "tag"), Is.False,
          "The default role must not grant 'tag'; otherwise this test proves nothing.");

        Assert.That(service.IsPermitted(UnprivilegedUser, null, "tag", new[] { "add", "@self", FreeTag }), Is.True);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "tag", new[] { "remove", "@self", FreeTag }), Is.True);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "tag", new[] { "add", "@self", GatedTag }), Is.False);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "tag", new[] { "add", "@self", UndefinedTag }), Is.False);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "tag", new[] { "show", "@self" }), Is.False);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "tag", null), Is.False);

        // 권한 면제는 다른 커맨드로 번지지 않는다.
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "tp", new[] { "@self", "0", "0", "0" }), Is.False);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "help", new string[0]), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void DatapackAliasInheritsTheExemptionOfItsTargetCommand()
    {
      var gameObject = new GameObject("ChatCommandService Alias Test");
      try
      {
        var service = gameObject.AddComponent<ChatCommandService>();
        service.Initialize(null);
        service.RegisterAlias("zzpickfree", "/tag add @self " + FreeTag, "test-pack");
        service.RegisterAlias("zzpickgated", "/tag add @self " + GatedTag, "test-pack");
        service.RegisterAlias("zzpickboth", "/tag add @self " + FreeTag + "; /tag add @self " + GatedTag, "test-pack");
        service.RegisterAlias("zzpickarg", "/tag add @self", "test-pack");
        service.RegisterAlias("zznested", "/zzpickfree", "test-pack");

        Assert.That(service.IsPermitted(UnprivilegedUser, null, "zzpickfree", new string[0]), Is.True);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "zzpickgated", new string[0]), Is.False);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "zzpickboth", new string[0]), Is.False);

        // 별칭에 전달한 인자는 마지막 대상 커맨드에 붙으므로 태그가 인자로 오는 별칭도 같은 판단을 받는다.
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "zzpickarg", new[] { FreeTag }), Is.True);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "zzpickarg", new[] { GatedTag }), Is.False);
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "zzpickarg", new string[0]), Is.False);

        // 별칭이 별칭을 가리켜도 대상 커맨드의 면제가 이어진다.
        Assert.That(service.IsPermitted(UnprivilegedUser, null, "zznested", new string[0]), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }
  }
}
