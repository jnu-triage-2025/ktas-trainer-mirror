using System;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 이 특성(Attribute)이 적용된 <see cref="Item"/> 파생 클래스는
  /// 3D 모델(프리팹)이 의도적으로 누락되었음을 선언합니다.
  ///
  /// <para>
  /// 적용 대상: Item 파생 클래스 (클래스 선언부 위)
  /// </para>
  /// <example>
  /// <code>
  /// [IntendedMissing3DModel]
  /// public class Scissors : MedicalItem
  /// {
  ///   public const string Identifier = "scissors";
  /// }
  /// </code>
  /// </example>
  ///
  /// <para>
  /// <c>Inherited = true</c> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
  /// </para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
  public sealed class IntendedMissing3DModelAttribute : Attribute { }

  /// <summary>
  /// 이 특성(Attribute)이 적용된 <see cref="Item"/> 파생 클래스는
  /// 아이콘 스프라이트가 의도적으로 누락되었음을 선언합니다.
  ///
  /// <para>
  /// 적용 대상: Item 파생 클래스 (클래스 선언부 위)
  /// </para>
  /// <example>
  /// <code>
  /// [IntendedMissingItemSprite]
  /// public class Gauze : MedicalItem
  /// {
  ///   public const string Identifier = "gauze";
  /// }
  /// </code>
  /// </example>
  ///
  /// <para>
  /// <c>Inherited = true</c> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
  /// </para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
  public sealed class IntendedMissingItemSpriteAttribute : Attribute { }
}
