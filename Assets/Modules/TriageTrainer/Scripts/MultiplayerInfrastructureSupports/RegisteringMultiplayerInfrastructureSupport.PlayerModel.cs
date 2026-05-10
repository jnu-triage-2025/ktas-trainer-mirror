using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
	public partial class RegisteringMultiplayerInfrastructureSupport
	{
		[Header("Player Model Registry")]
		[SerializeField] private PlayerModelRegistryRequirementsSO _playerModelRegistryRequirementsSO;

		private void Awake_PlayerModel()
		{
			RegisterAllPlayerModels();
			ValidatePlayerModelResources();
		}

		private void RegisterAllPlayerModels()
		{
			if (_playerModelRegistryRequirementsSO == null)
			{
				Debug.LogWarning("[RegisteringMultiplayerInfrastructureSupport] PlayerModelRegistryRequirementsSO is not assigned.", this);
				return;
			}

			var requirements = _playerModelRegistryRequirementsSO.playerModelRegistryRequirements;
			if (requirements == null || requirements.Length == 0)
			{
				Debug.LogWarning("[RegisteringMultiplayerInfrastructureSupport] No player model registry requirements configured.", this);
				return;
			}

			int registeredCount = 0;

			for (int i = 0; i < requirements.Length; i++)
			{
				var req = requirements[i];

				if (string.IsNullOrWhiteSpace(req.identifier))
				{
					Debug.LogWarning($"[RegisteringMultiplayerInfrastructureSupport] PlayerModel[{i}] has empty identifier.", this);
					continue;
				}

				if (req.prefab == null)
				{
					Debug.LogWarning($"[RegisteringMultiplayerInfrastructureSupport] PlayerModel[{i}] '{req.identifier}' has null prefab.", this);
					continue;
				}

				Registry.Register(RegistryType.PlayerModel, req.identifier, req.prefab);
				registeredCount++;
			}

			Debug.Log($"[RegisteringMultiplayerInfrastructureSupport] Registered {registeredCount} player model entries.", this);
		}

		private void ValidatePlayerModelResources()
		{
			if (_playerModelRegistryRequirementsSO == null)
				return;

			var requirements = _playerModelRegistryRequirementsSO.playerModelRegistryRequirements;
			if (requirements == null || requirements.Length == 0)
				return;

			int invalidCount = 0;

			for (int i = 0; i < requirements.Length; i++)
			{
				var req = requirements[i];
				if (string.IsNullOrWhiteSpace(req.identifier) || req.prefab == null)
					continue;

				if (!req.prefab.TryGetComponent<IPlayerCharacterModelObject>(out _))
				{
					Debug.LogWarning(
						$"[RegisteringMultiplayerInfrastructureSupport] PlayerModel[{i}] '{req.identifier}' prefab does not have IPlayerModelObject component.",
						req.prefab);
					invalidCount++;
				}
			}

			if (invalidCount == 0)
			{
				Debug.Log("[RegisteringMultiplayerInfrastructureSupport] Player model resource validation completed successfully.", this);
			}
			else
			{
				Debug.LogWarning($"[RegisteringMultiplayerInfrastructureSupport] Player model resource validation completed with {invalidCount} invalid prefabs.", this);
			}
		}
	}
}
