using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class NPCAssembler : MonoBehaviour {
	[Header("Humanoid")]
	[Tooltip("Instancia de Humanoid base")]
	public GameObject humanoid;

	[Header("Config")]
	public NPCConfigData randomConfig;
	public bool assembleOnStart = false;

	[Header("Outfit")]
	[SerializeField, HideInInspector]
	private readonly List<GameObject> generatedPieces = new();

	private Animator baseAnimator;
	private Transform skeletonRoot;

	private void Start() {
		if (assembleOnStart && randomConfig != null) {
			Setup();
		}
	}

	public void Setup() {
		if (!ValidateSetup()) {
			return;
		}

		GenerateRandomNPC(randomConfig);
	}

	private bool ValidateSetup() {
		if (humanoid == null) {
			Debug.LogError("NPCAssembler:  no hay humanoide", this);
			return false;
		}

		baseAnimator = humanoid.GetComponentInChildren<Animator>(true);
		if (baseAnimator == null || baseAnimator.avatar == null) {
			Debug.LogError("NPCAssembler:  humanoide mal configurado.", this);
			return false;
		}

		skeletonRoot = baseAnimator.transform;
		return true;
	}

	public void GenerateRandomNPC(NPCConfigData config) {
		if (config == null) {
			Debug.LogWarning("NPCAssembler:  configdata no proporcionado.", this);
			return;
		}

		if (!ValidateSetup()) {
			return;
		}

		ClearCurrentPieces();

		AttachPiece(GetRandomPiece(config.possibleHair));
		AttachPiece(GetRandomPiece(config.possibleShirts));
		AttachPiece(GetRandomPiece(config.possiblePants));

		ApplyRandomSkin(config.possibleSkins);
	}

	private ClothItemData GetRandomPiece(ClothItemData[] collection) {
		if (collection == null || collection.Length == 0) {
			return null;
		}

		return collection[UnityEngine.Random.Range(0, collection.Length)];
	}

	private void AttachPiece(ClothItemData itemData) {
		if (itemData == null || itemData.clothingPrefab == null) {
			return;
		}

		GameObject piece = Instantiate(itemData.clothingPrefab, transform, false);
		piece.name = itemData.clothingPrefab.name;

		Animator[] clothingAnimators = piece.GetComponentsInChildren<Animator>(true);
		foreach (Animator animator in clothingAnimators) {
			animator.enabled = false;
		}

		if (!BindSkinnedMeshes(piece)) {
			Debug.LogWarning($"NPCAssembler:  error al aplicar [{piece.name}]", piece);
		}

		ApplyRandomMaterial(piece, itemData.availableMaterials);
		generatedPieces.Add(piece);

#if UNITY_EDITOR
		if (!Application.isPlaying) {
			EditorUtility.SetDirty(this);
		}
#endif
	}

	private bool BindSkinnedMeshes(GameObject piece) {
		Dictionary<string, Transform> sharedBones = new();
		Transform[] humanoidTransforms = humanoid.GetComponentsInChildren<Transform>(true);
		foreach (Transform bone in humanoidTransforms) {
			if (!sharedBones.ContainsKey(bone.name)) {
				sharedBones.Add(bone.name, bone);
			}
		}

		bool allBound = true;
		SkinnedMeshRenderer[] renderers = piece.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		foreach (SkinnedMeshRenderer smr in renderers) {
			Transform[] originalBones = smr.bones;
			Transform[] mappedBones = new Transform[originalBones.Length];

			for (int i = 0; i < originalBones.Length; i++) {
				Transform originalBone = originalBones[i];

				if (originalBone != null && sharedBones.TryGetValue(originalBone.name, out Transform mapped)) {
					mappedBones[i] = mapped;
				} else {
					mappedBones[i] = null;
					allBound = false;

					Debug.LogWarning($"Falta hueso [{originalBone?.name}] en humanoide para [{smr.name}].", smr);
				}
			}

			smr.bones = mappedBones;

			if (smr.rootBone != null && sharedBones.TryGetValue(smr.rootBone.name, out Transform mappedRoot)) {
				smr.rootBone = mappedRoot;
			} else {
				smr.rootBone = skeletonRoot;
			}
		}

		return allBound;
	}

	private void ApplyRandomMaterial(GameObject piece, Material[] availableMaterials) {
		if (availableMaterials == null || availableMaterials.Length == 0) {
			return;
		}

		Material chosen = availableMaterials[UnityEngine.Random.Range(0, availableMaterials.Length)];
		if (chosen == null) {
			return;
		}

		Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer renderer in renderers) {
			renderer.sharedMaterial = chosen;
		}
	}

	private void ApplyRandomSkin(SkinVariantData[] skins) {
		if (skins == null || skins.Length == 0 || humanoid == null) {
			return;
		}

		SkinVariantData skin = skins[UnityEngine.Random.Range(0, skins.Length)];
		if (skin == null) {
			return;
		}

		Material head = (skin.heads != null && skin.heads.Length > 0)
			? skin.heads[UnityEngine.Random.Range(0, skin.heads.Length)]
			: null;

		Material body = skin.body;

		SkinnedMeshRenderer[] renderers = humanoid.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		foreach (SkinnedMeshRenderer renderer in renderers) {
			Material[] materials = renderer.sharedMaterials;
			if (materials.Length > 0) {
				materials[0] = body;
			}

			if (materials.Length > 1) {
				materials[1] = head;
			}

			renderer.sharedMaterials = materials;
		}
	}

	public void ClearCurrentPieces() {
		for (int i = generatedPieces.Count - 1; i >= 0; i--) {
			GameObject piece = generatedPieces[i];

			if (piece == null) {
				continue;
			}

			if (Application.isPlaying) {
				Destroy(piece);
			} else {
#if UNITY_EDITOR
				Undo.DestroyObjectImmediate(piece);
#else
				DestroyImmediate(piece);
#endif
			}
		}

		generatedPieces.Clear();
	}

	public void ResetToNormal() {
		ClearCurrentPieces();

		if (humanoid != null) {
			SkinnedMeshRenderer[] renderers = humanoid.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			foreach (SkinnedMeshRenderer renderer in renderers) {
				renderer.enabled = true;
			}
		}

#if UNITY_EDITOR
		if (!Application.isPlaying) {
			EditorUtility.SetDirty(this);
		}
#endif
	}
}
