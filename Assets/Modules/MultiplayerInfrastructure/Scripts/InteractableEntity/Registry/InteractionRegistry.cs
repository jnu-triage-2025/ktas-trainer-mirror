using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>레지스트리 항목. 코드 정의와 데이터 정의를 병합한 유효 정의와 핸들러를 가진다.</summary>
  public sealed class InteractionRegistryEntry
  {
    public InteractionAddress Address { get; }
    public InteractionDefinition CodeDefinition { get; internal set; }
    public InteractionDefinition DataDefinition { get; internal set; }
    public string DataScenarioIdentifier { get; internal set; }
    public InteractionDefinition Definition { get; private set; }
    public IInteract Handler { get; internal set; }
    public bool HandlerIsGeneric { get; internal set; }
    public bool RegisteredOutsideInitCycle { get; internal set; }
    public int Sequence { get; internal set; }

    internal InteractionRegistryEntry(InteractionAddress address)
    {
      Address = address;
    }

    public string Source => CodeDefinition != null
      ? (DataDefinition != null ? $"code+scenario:{DataScenarioIdentifier}" : "code")
      : (DataDefinition != null ? $"scenario:{DataScenarioIdentifier}" : "(none)");

    internal void Rebuild()
    {
      if (CodeDefinition != null)
        Definition = CodeDefinition.MergeOverlay(DataDefinition);
      else if (DataDefinition != null)
        Definition = DataDefinition.Clone();
      else
        Definition = null;

      if (Definition != null)
        Definition.Entity = ScenarioEntityReference.ForIdentifier(Address.EntityIdentifier);
    }
  }

  /// <summary>
  /// 런타임 인터렉션 레지스트리. 정의는 엔티티 초기화 사이클(코드 리터럴)과 시나리오 초기화 사이클(시나리오 데이터)
  /// 안에서만 등록되고, 그 뒤에는 가시성만 조건과 오버라이드로 바뀐다.
  ///
  /// <para>
  /// 등록은 각 피어가 로컬로 수행한다(정의는 결정적 데이터). 가시성 오버라이드는 서버 권위이며
  /// <see cref="InteractionVisibilityState"/> 와 <see cref="ScenarioNetworkRelay"/> 가 복제한다.
  /// </para>
  /// </summary>
  public static class InteractionRegistry
  {
    private sealed class ReferenceComparer : IEqualityComparer<IInteract>
    {
      public bool Equals(IInteract x, IInteract y) => ReferenceEquals(x, y);
      public int GetHashCode(IInteract obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    private sealed class PendingDataDefinition
    {
      public string ScenarioIdentifier;
      public InteractionDefinition Definition;
    }

    private static readonly Dictionary<string, InteractionRegistryEntry> Entries =
      new Dictionary<string, InteractionRegistryEntry>(StringComparer.Ordinal);
    private static readonly Dictionary<IInteract, InteractionRegistryEntry> EntriesByHandler =
      new Dictionary<IInteract, InteractionRegistryEntry>(new ReferenceComparer());
    private static readonly List<PendingDataDefinition> PendingData = new List<PendingDataDefinition>();
    private static readonly HashSet<Type> WarnedUnregisteredHandlerTypes = new HashSet<Type>();

    private static int _entityInitDepth;
    private static int _scenarioInitDepth;
    private static int _sequence;
    private static bool _hooked;
    private static bool _suppressChanged;

    /// <summary>정의 목록이 바뀌었을 때(등록·병합·제거).</summary>
    public static event Action Changed;

    /// <summary>힌트 목록을 다시 계산해야 할 때. 정의 변경, 오버라이드 변경, 조건 입력 변경이 모두 여기로 모인다.</summary>
    public static event Action HintRefreshRequested;

    /// <summary>등록 항목이 수행되었을 때(범용 핸들러 또는 성공을 보고한 Custom 핸들러). 연출 코드가 구독한다.</summary>
    public static event Action<InteractionRegistryEntry, PlayerController> Interacted;

    public static bool IsInitCycleActive => _entityInitDepth > 0 || _scenarioInitDepth > 0;

    public static IEnumerable<InteractionRegistryEntry> AllEntries => Entries.Values;

    public static int Count => Entries.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
      ClearAll();
      _entityInitDepth = 0;
      _scenarioInitDepth = 0;
      WarnedUnregisteredHandlerTypes.Clear();
      Changed = null;
      HintRefreshRequested = null;
      Interacted = null;
      EnsureHooked(force: true);
    }

    private static void EnsureHooked(bool force = false)
    {
      if (_hooked && !force)
        return;
      if (_hooked)
      {
        Registry.Registry.OnEntryRegistered -= HandleRegistryEntryRegistered;
        PlayerTagService.TagsChanged -= HandleTagsChanged;
        InteractionVisibilityState.Changed -= HandleVisibilityStateChanged;
      }
      _hooked = true;
      Registry.Registry.OnEntryRegistered += HandleRegistryEntryRegistered;
      PlayerTagService.TagsChanged += HandleTagsChanged;
      InteractionVisibilityState.Changed += HandleVisibilityStateChanged;
    }

    // ── 초기화 사이클 ─────────────────────────────────────────────────────

    private sealed class CycleScope : IDisposable
    {
      private readonly bool _entity;
      private bool _disposed;

      public CycleScope(bool entity)
      {
        _entity = entity;
        if (entity) _entityInitDepth++; else _scenarioInitDepth++;
      }

      public void Dispose()
      {
        if (_disposed) return;
        _disposed = true;
        if (_entity) _entityInitDepth = Math.Max(0, _entityInitDepth - 1);
        else _scenarioInitDepth = Math.Max(0, _scenarioInitDepth - 1);
      }
    }

    /// <summary>엔티티 초기화 사이클을 연다. 이 범위 안의 등록은 경고 없이 정상 등록된다.</summary>
    public static IDisposable BeginEntityInitCycle(string entityIdentifier)
    {
      EnsureHooked();
      return new CycleScope(entity: true);
    }

    /// <summary>시나리오 초기화 사이클을 연다.</summary>
    public static IDisposable BeginScenarioInitCycle(string scenarioIdentifier)
    {
      EnsureHooked();
      return new CycleScope(entity: false);
    }

    // ── 코드 리터럴 정의 ─────────────────────────────────────────────────

    /// <summary>엔티티 컴포넌트가 선언한 코드 리터럴 정의를 엔티티 초기화 사이클 안에서 등록한다.</summary>
    public static void DeclareCode(string entityIdentifier, IInteractionDefinitionSource source)
    {
      if (source == null || string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      using (BeginEntityInitCycle(entityIdentifier))
      {
        _suppressChanged = true;
        try
        {
          foreach (var declaration in source.DeclareInteractions())
          {
            if (declaration?.Definition == null)
              continue;
            var definition = declaration.Definition;
            if (definition.Entity == null || definition.Entity.IsEmpty)
              definition.Entity = ScenarioEntityReference.ForIdentifier(entityIdentifier);
            DeclareCode(definition, declaration.Handler);
          }
        }
        finally
        {
          _suppressChanged = false;
        }
      }
      RaiseChanged();
    }

    /// <summary>코드 리터럴 정의 하나를 등록한다. Custom 종류는 핸들러가 필요하다.</summary>
    public static void DeclareCode(InteractionDefinition definition, IInteract handler)
    {
      EnsureHooked();
      if (definition == null)
        return;
      string entityIdentifier = definition.Entity?.Identifier?.Trim();
      if (string.IsNullOrWhiteSpace(entityIdentifier) || string.IsNullOrWhiteSpace(definition.InteractionIdentifier))
      {
        Debug.LogWarning("[InteractionRegistry] Code definition requires entity identifier and interaction identifier.");
        return;
      }

      var address = new InteractionAddress(entityIdentifier, definition.InteractionIdentifier);
      var entry = GetOrCreateEntry(address);
      entry.CodeDefinition = definition.Clone();
      if (handler != null)
      {
        DetachHandler(entry);
        entry.Handler = handler;
        entry.HandlerIsGeneric = false;
        EntriesByHandler[handler] = entry;
      }
      entry.Rebuild();
      EnsureHandler(entry);
      WarnIfOutsideInitCycle(entry);
      RaiseChanged();
    }

    /// <summary>엔티티가 사라질 때 그 엔티티의 코드 리터럴 정의를 제거한다.</summary>
    public static void RemoveCodeDefinitions(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;
      entityIdentifier = entityIdentifier.Trim();
      var removed = new List<string>();
      foreach (var pair in Entries)
      {
        var entry = pair.Value;
        if (!string.Equals(entry.Address.EntityIdentifier, entityIdentifier, StringComparison.Ordinal) || entry.CodeDefinition == null)
          continue;
        entry.CodeDefinition = null;
        if (!entry.HandlerIsGeneric)
          DetachHandler(entry);
        entry.Rebuild();
        if (entry.Definition == null)
          removed.Add(pair.Key);
        else
          EnsureHandler(entry);
      }
      foreach (var key in removed)
        RemoveEntry(key);
      if (removed.Count > 0)
        RaiseChanged();
    }

    // ── 시나리오 데이터 정의 ─────────────────────────────────────────────

    /// <summary>
    /// 시나리오 데이터 정의를 등록한다. 대상 엔티티가 아직 없으면 보류했다가 그 엔티티의 초기화 때 적용하고,
    /// 태그 참조 정의는 시나리오가 끝날 때까지 새로 나타나는 엔티티에도 적용한다.
    /// </summary>
    public static void ApplyScenarioDefinitions(string scenarioIdentifier, IReadOnlyList<InteractionDefinition> definitions)
    {
      EnsureHooked();
      ClearScenarioDefinitions(scenarioIdentifier);
      if (definitions == null || definitions.Count == 0)
        return;

      _suppressChanged = true;
      try
      {
        for (int i = 0; i < definitions.Count; i++)
        {
          var definition = definitions[i];
          if (definition == null || definition.Entity == null || definition.Entity.IsEmpty
              || string.IsNullOrWhiteSpace(definition.InteractionIdentifier))
            continue;

          var pending = new PendingDataDefinition { ScenarioIdentifier = scenarioIdentifier, Definition = definition.Clone() };
          PendingData.Add(pending);

          if (!definition.Entity.IsTagReference)
          {
            string identifier = definition.Entity.Identifier.Trim();
            if (Registry.Registry.TryGetEntity(identifier, out var entity) && entity?.GameObject != null)
              ApplyDataToEntity(pending, identifier);
            continue;
          }

          foreach (var pair in Registry.Registry.GetAllEntities())
          {
            if (definition.Entity.Matches(pair.Key))
              ApplyDataToEntity(pending, pair.Key);
          }
        }
      }
      finally
      {
        _suppressChanged = false;
      }
      RaiseChanged();
    }

    private static void ApplyDataToEntity(PendingDataDefinition pending, string entityIdentifier)
    {
      var address = new InteractionAddress(entityIdentifier, pending.Definition.InteractionIdentifier);
      var entry = GetOrCreateEntry(address);
      entry.DataDefinition = pending.Definition.Clone();
      entry.DataScenarioIdentifier = pending.ScenarioIdentifier;
      entry.Rebuild();
      EnsureHandler(entry);
      WarnIfOutsideInitCycle(entry);
    }

    /// <summary>시나리오가 끝날 때 그 시나리오의 데이터 정의와 보류 정의를 모두 제거한다.</summary>
    public static void ClearScenarioDefinitions(string scenarioIdentifier)
    {
      PendingData.RemoveAll(pending => string.Equals(pending.ScenarioIdentifier, scenarioIdentifier, StringComparison.Ordinal));
      var removed = new List<string>();
      foreach (var pair in Entries)
      {
        var entry = pair.Value;
        if (entry.DataDefinition == null
            || !string.Equals(entry.DataScenarioIdentifier, scenarioIdentifier, StringComparison.Ordinal))
          continue;
        entry.DataDefinition = null;
        entry.DataScenarioIdentifier = null;
        entry.Rebuild();
        if (entry.Definition == null)
          removed.Add(pair.Key);
        else
          EnsureHandler(entry);
      }
      foreach (var key in removed)
        RemoveEntry(key);
      if (removed.Count > 0 || PendingData.Count > 0)
        RaiseChanged();
    }

    // ── 조회 ────────────────────────────────────────────────────────────

    public static bool TryGet(InteractionAddress address, out InteractionRegistryEntry entry)
      => Entries.TryGetValue(address.Key, out entry);

    public static bool TryGet(string addressKey, out InteractionRegistryEntry entry)
    {
      entry = null;
      return !string.IsNullOrWhiteSpace(addressKey) && Entries.TryGetValue(addressKey, out entry);
    }

    public static bool TryGetByHandler(IInteract handler, out InteractionRegistryEntry entry)
    {
      entry = null;
      return handler != null && EntriesByHandler.TryGetValue(handler, out entry);
    }

    /// <summary>핸들러가 속한 항목의 유효 정의. Custom 핸들러가 완료 신호·요구 아이템·부가 문자열을 읽을 때 쓴다.</summary>
    public static bool TryGetDefinition(IInteract handler, out InteractionDefinition definition)
    {
      definition = TryGetByHandler(handler, out var entry) ? entry.Definition : null;
      return definition != null;
    }

    /// <summary>
    /// 시나리오 데이터가 명시한 표시 정보. 코드 핸들러의 동적 문구를 덮지 않도록 데이터 정의가 있을 때만 돌려준다.
    /// 힌트 UI 와 표시 우선순위 정렬이 참조한다.
    /// </summary>
    public static bool TryGetDataDisplay(IInteract handler, out InteractionDisplay display)
    {
      display = TryGetByHandler(handler, out var entry) ? entry.DataDefinition?.Display : null;
      return display != null;
    }

    /// <summary>엔티티의 등록 항목 핸들러를 선언 순서대로 모은다. 엔티티의 <see cref="IInteractable.Interacts"/> 가 호출한다.</summary>
    public static void CollectInteractsForEntity(string entityIdentifier, List<IInteract> buffer)
    {
      if (buffer == null || string.IsNullOrWhiteSpace(entityIdentifier))
        return;
      entityIdentifier = entityIdentifier.Trim();
      var matched = new List<InteractionRegistryEntry>();
      foreach (var entry in Entries.Values)
      {
        if (entry.Handler != null
            && string.Equals(entry.Address.EntityIdentifier, entityIdentifier, StringComparison.Ordinal))
          matched.Add(entry);
      }
      matched.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
      for (int i = 0; i < matched.Count; i++)
        buffer.Add(matched[i].Handler);
    }

    public static IEnumerable<InteractionRegistryEntry> EntriesForEntity(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        yield break;
      entityIdentifier = entityIdentifier.Trim();
      foreach (var entry in Entries.Values)
      {
        if (string.Equals(entry.Address.EntityIdentifier, entityIdentifier, StringComparison.Ordinal))
          yield return entry;
      }
    }

    // ── 가시성 ──────────────────────────────────────────────────────────

    /// <summary>관찰자 기준 가시성. 오버라이드(플레이어별, 전역) → 조건 → 초기값 순서로 판정한다.</summary>
    public static bool IsVisible(InteractionRegistryEntry entry, PlayerController viewer, out string reason)
      => IsVisible(entry, ScenarioConditionContext.ForPlayer(viewer), out reason);

    public static bool IsVisibleForPlayerIdentifier(InteractionAddress address, string playerIdentifier, out string reason)
    {
      if (!TryGet(address, out var entry))
      {
        reason = $"'{address}' is not registered.";
        return false;
      }
      return IsVisible(entry, ScenarioConditionContext.ForPlayerIdentifier(playerIdentifier), out reason);
    }

    public static bool IsVisible(InteractionRegistryEntry entry, in ScenarioConditionContext context, out string reason)
    {
      reason = null;
      if (entry?.Definition == null)
      {
        reason = "entry has no definition.";
        return false;
      }

      if (InteractionVisibilityState.TryGetOverride(entry.Address.Key, context.PlayerIdentifier, out bool overridden))
      {
        reason = overridden ? "override: show" : "override: hide";
        return overridden;
      }

      var definition = entry.Definition;
      if (definition.HasVisibilityConditions)
      {
        bool passed = ScenarioConditionEvaluator.Evaluate(definition.VisibilityConditions, definition.MatchMode, context, out string failure);
        reason = passed ? "conditions satisfied" : failure;
        return passed;
      }

      reason = definition.InitialVisible ? "initial: visible" : "initial: hidden";
      return definition.InitialVisible;
    }

    /// <summary>
    /// 가시성 오버라이드를 서버 권위로 기록한다. 서버면 직접 기록하고 미러링하며, 클라이언트면 서버에 위임한다.
    /// 플레이어 범위는 대상 플레이어 식별자마다 한 번씩 기록한다.
    /// </summary>
    public static void SetVisibilityOverride(InteractionAddress address, InteractionVisibilityOverride value,
      InteractionVisibilityScope scope, IReadOnlyList<string> playerIdentifiers = null)
    {
      if (!address.IsValid)
        return;
      if (scope == InteractionVisibilityScope.Global)
      {
        ScenarioNetworkRelay.ApplyInteractionVisibilityAuthoritative(address.Key, scope, null, value);
        return;
      }
      if (playerIdentifiers == null)
        return;
      for (int i = 0; i < playerIdentifiers.Count; i++)
      {
        if (!string.IsNullOrWhiteSpace(playerIdentifiers[i]))
          ScenarioNetworkRelay.ApplyInteractionVisibilityAuthoritative(address.Key, scope, playerIdentifiers[i].Trim(), value);
      }
    }

    /// <summary>
    /// 입력 디스패처가 Custom 핸들러 수행 뒤 호출한다. 범용 핸들러는 실제 완료 시점에 직접 통지한다.
    /// </summary>
    public static void NotifyCustomInteracted(IInteract handler, PlayerController player)
    {
      if (TryGetByHandler(handler, out var entry) && !entry.HandlerIsGeneric)
        NotifyInteracted(handler, player);
    }

    /// <summary>
    /// 실제 완료 시 호출된다. 범용 핸들러이거나 <see cref="IInteractOutcomeSource"/> 가 성공을 보고한
    /// Custom 핸들러에 대해 수행 뒤 가시성 처리(afterInteract)를 적용한다.
    /// </summary>
    public static void NotifyInteracted(IInteract handler, PlayerController player)
    {
      if (!TryGetByHandler(handler, out var entry) || entry.Definition == null)
        return;
      if (!entry.HandlerIsGeneric)
      {
        if (handler is not IInteractOutcomeSource outcome || !outcome.LastInteractSucceeded)
          return;
      }

      try { Interacted?.Invoke(entry, player); }
      catch (Exception ex) { Debug.LogException(ex); }

      switch (entry.Definition.AfterInteract)
      {
        case InteractionAfterInteract.HideForAll:
          SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Hide, InteractionVisibilityScope.Global);
          break;
        case InteractionAfterInteract.HideForPlayer:
          string playerIdentifier = player != null ? player.UserIdentifier : null;
          if (!string.IsNullOrWhiteSpace(playerIdentifier))
            SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Hide, InteractionVisibilityScope.Player, new[] { playerIdentifier });
          break;
      }
    }

    /// <summary>엔티티의 모든 항목에서 가시성 오버라이드(트리거·수행 뒤 처리)를 지운다. 수동 진입 준비처럼 단계를 되돌릴 때 쓴다.</summary>
    public static void ResetOverridesForEntity(string entityIdentifier)
    {
      foreach (var entry in EntriesForEntity(entityIdentifier))
      {
        SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Reset, InteractionVisibilityScope.Global);
        foreach (var playerPair in InteractionVisibilityState.PlayerOverrides)
        {
          if (playerPair.Value.ContainsKey(entry.Address.Key))
            SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Reset, InteractionVisibilityScope.Player, new[] { playerPair.Key });
        }
      }
    }

    /// <summary>
    /// 엔티티 코드가 자기 초기화 사이클에서 고정 태그를 부여한다. 서버면 권위 기록(복제됨), 네트워크가 없으면 로컬 기록,
    /// 클라이언트면 서버 복제를 기다린다(아무것도 하지 않는다).
    /// </summary>
    public static void AssignEntityTag(string entityIdentifier, string tag)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || string.IsNullOrWhiteSpace(tag))
        return;
      if (FishNet.InstanceFinder.IsServerStarted)
      {
        PlayerTagService.AddTagToIdentifier(entityIdentifier, tag);
        return;
      }
      if (!FishNet.InstanceFinder.IsOffline)
        return;
      var current = new List<string>(PlayerTagService.GetTagsByIdentifier(entityIdentifier));
      if (current.Contains(tag))
        return;
      current.Add(tag);
      PlayerTagService.ReplaceTags(entityIdentifier, current);
    }

    /// <summary>조건 입력이 바뀐 코드가 힌트 목록 재계산을 요청한다.</summary>
    public static void RequestHintRefresh() => HintRefreshRequested?.Invoke();

    /// <summary>레지스트리에 없는 핸들러를 힌트 수집 경로가 만났을 때 에디터에서 한 번 경고한다(마이그레이션 진단).</summary>
    public static void WarnUnregisteredHandler(IInteract handler)
    {
      if (handler == null || handler is IInteractionRegistryExempt || !Application.isEditor)
        return;
      var type = handler.GetType();
      if (!WarnedUnregisteredHandlerTypes.Add(type))
        return;
      Debug.LogWarning($"[InteractionRegistry] '{type.Name}' is not registered in the interaction registry; using legacy visibility path.");
    }

    // ── 정리 ────────────────────────────────────────────────────────────

    public static void ClearAll()
    {
      foreach (var entry in Entries.Values)
        DisposeGenericHandler(entry);
      Entries.Clear();
      EntriesByHandler.Clear();
      PendingData.Clear();
      InteractionVisibilityState.ClearAll();
      RaiseChanged();
    }

    // ── 내부 ────────────────────────────────────────────────────────────

    private static InteractionRegistryEntry GetOrCreateEntry(InteractionAddress address)
    {
      if (!Entries.TryGetValue(address.Key, out var entry))
      {
        entry = new InteractionRegistryEntry(address) { Sequence = ++_sequence };
        Entries.Add(address.Key, entry);
      }
      return entry;
    }

    private static void RemoveEntry(string key)
    {
      if (!Entries.TryGetValue(key, out var entry))
        return;
      DetachHandler(entry);
      Entries.Remove(key);
    }

    private static void DetachHandler(InteractionRegistryEntry entry)
    {
      if (entry.Handler == null)
        return;
      EntriesByHandler.Remove(entry.Handler);
      DisposeGenericHandler(entry);
      entry.Handler = null;
      entry.HandlerIsGeneric = false;
    }

    private static void DisposeGenericHandler(InteractionRegistryEntry entry)
    {
      if (entry.HandlerIsGeneric && entry.Handler is IDisposable disposable)
      {
        try { disposable.Dispose(); }
        catch (Exception ex) { Debug.LogException(ex); }
      }
    }

    /// <summary>범용 종류인데 핸들러가 없으면 만들고, Custom 인데 핸들러가 범용이면 떼어 낸다.</summary>
    private static void EnsureHandler(InteractionRegistryEntry entry)
    {
      var definition = entry.Definition;
      if (definition == null)
        return;

      if (definition.Kind == InteractionKind.Custom)
      {
        if (entry.HandlerIsGeneric)
          DetachHandler(entry);
        if (entry.Handler == null)
          TryCreateFactoryHandler(entry);
        return;
      }

      if (entry.Handler != null && !entry.HandlerIsGeneric)
        return; // 코드가 준 핸들러가 범용 종류를 대신한다.

      if (entry.Handler is RegistryInteractBase generic && generic.Kind == definition.Kind)
      {
        generic.Refresh();
        return;
      }

      DetachHandler(entry);
      var handler = RegistryInteractBase.Create(entry);
      if (handler == null)
        return;
      entry.Handler = handler;
      entry.HandlerIsGeneric = true;
      EntriesByHandler[handler] = entry;
    }

    /// <summary>데이터로만 선언된 Custom 정의의 핸들러를 엔티티의 <see cref="IInteractionHandlerFactory"/> 에서 만든다.</summary>
    private static void TryCreateFactoryHandler(InteractionRegistryEntry entry)
    {
      if (!Registry.Registry.TryGetEntity(entry.Address.EntityIdentifier, out var descriptor) || descriptor?.GameObject == null)
        return;
      var factories = descriptor.GameObject.GetComponentsInChildren<IInteractionHandlerFactory>(true);
      for (int i = 0; i < factories.Length; i++)
      {
        if (factories[i] == null || !factories[i].TryCreateInteractionHandler(entry.Definition, out var handler) || handler == null)
          continue;
        entry.Handler = handler;
        entry.HandlerIsGeneric = false;
        EntriesByHandler[handler] = entry;
        return;
      }
    }

    private static void WarnIfOutsideInitCycle(InteractionRegistryEntry entry)
    {
      if (IsInitCycleActive)
        return;
      entry.RegisteredOutsideInitCycle = true;
      if (Application.isEditor)
        Debug.LogWarning($"[InteractionRegistry] '{entry.Address}' was registered outside an initialization cycle.");
    }

    private static void HandleRegistryEntryRegistered(RegistryType type, string identifier, object value)
    {
      if (type != RegistryType.Entity || string.IsNullOrWhiteSpace(identifier))
        return;
      ApplyPendingForEntity(identifier);
    }

    private static void HandleTagsChanged(string identifier)
    {
      if (!string.IsNullOrWhiteSpace(identifier) && Registry.Registry.TryGetEntity(identifier, out _))
        ApplyPendingForEntity(identifier);
      RequestHintRefresh();
    }

    private static void ApplyPendingForEntity(string identifier)
    {
      bool applied = false;
      _suppressChanged = true;
      try
      {
        using (BeginEntityInitCycle(identifier))
        {
          for (int i = 0; i < PendingData.Count; i++)
          {
            var pending = PendingData[i];
            if (pending.Definition.Entity == null || !pending.Definition.Entity.Matches(identifier))
              continue;
            var address = new InteractionAddress(identifier, pending.Definition.InteractionIdentifier);
            if (Entries.TryGetValue(address.Key, out var existing) && existing.DataDefinition != null)
            {
              if (existing.Handler == null)
              {
                EnsureHandler(existing);
                applied |= existing.Handler != null;
              }
              continue;
            }
            ApplyDataToEntity(pending, identifier);
            applied = true;
          }
        }
      }
      finally
      {
        _suppressChanged = false;
      }
      if (applied)
        RaiseChanged();
    }

    private static void HandleVisibilityStateChanged(string addressKey, string playerIdentifier) => RequestHintRefresh();

    private static void RaiseChanged()
    {
      if (_suppressChanged)
        return;
      Changed?.Invoke();
      RequestHintRefresh();
    }
  }
}
