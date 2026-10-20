using UnityEngine;

[CreateAssetMenu(fileName = "ItemRopa", menuName = "NPC/Item Ropa")]
public class ClothItemData : ScriptableObject {
	public GameObject clothingPrefab;
	public Material[] availableMaterials;
};
