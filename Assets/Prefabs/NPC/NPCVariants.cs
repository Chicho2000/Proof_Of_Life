using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCVariant", menuName = "NPC/Variante NPC")]
public class NPCConfigData : ScriptableObject {
	[Header("Posibles opciones para este tipo de NPC")]
	public ClothItemData[] possibleHair;
	public ClothItemData[] possibleShirts;
	public ClothItemData[] possiblePants;
	public Material[] possibleSkins;
}
