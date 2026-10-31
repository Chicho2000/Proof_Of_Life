using TMPro;
using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(DialogoNPC))]
public class NPCProximityDialogue : MonoBehaviour
{
    [SerializeField] private GameObject dialogueCanvasPrefab;
    [SerializeField, Min(0.1f)] private float speakingRadius = 5f;

    private DialogoNPC dialogue;
    private NPCBase npc;
    private NPCSuspicionSystem suspicion;
    private NPCHealth health;
    private Transform player;
    private bool spokeDuringVisit;

    private void Awake()
    {
        dialogue = GetComponent<DialogoNPC>();
        npc = GetComponent<NPCBase>();
        suspicion = GetComponent<NPCSuspicionSystem>();
        health = GetComponent<NPCHealth>();

        GameObject canvas = dialogue.canvasDialogo != null
            ? dialogue.canvasDialogo
            : FindExistingCanvas();

        if (canvas == null && dialogueCanvasPrefab == null)
        {
            Debug.LogWarning($"[NPCProximityDialogue] {name}: falta el prefab del globo.", this);
            return;
        }

        if (canvas == null) canvas = Instantiate(dialogueCanvasPrefab, transform);
        dialogue.canvasDialogo = canvas;
        if (dialogue.textoNombre == null)
            dialogue.textoNombre = FindChild(canvas.transform, "NPC_Nombre")?.GetComponent<TMP_Text>();
        if (dialogue.textoDialogo == null)
            dialogue.textoDialogo = FindChild(canvas.transform, "Texto_Dialogo")?.GetComponent<TMP_Text>();
        if (dialogue.barraLateral == null)
            dialogue.barraLateral = FindChild(canvas.transform, "BarraLateral")?.GetComponent<RectTransform>();
        if (dialogue.fondoOscuro == null)
            dialogue.fondoOscuro = FindChild(canvas.transform, "Fondo_Texto")?.GetComponent<RectTransform>();
        if (dialogue.headReference == null) dialogue.headReference = FindHead();

        if (dialogue.textoDialogo == null || dialogue.textoNombre == null)
            Debug.LogWarning($"[NPCProximityDialogue] {name}: faltan campos de texto en el globo.", this);
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f || dialogue == null || dialogue.canvasDialogo == null)
        {
            return;
        }

        if (player == null)
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null) return;
            player = playerHealth.transform;
        }

        if ((player.position - transform.position).sqrMagnitude > speakingRadius * speakingRadius)
        {
            spokeDuringVisit = false;
            return;
        }

        if (spokeDuringVisit || string.IsNullOrWhiteSpace(dialogue.frasePredeterminada) || !IsCalm())
        {
            return;
        }

        dialogue.MostrarFrasePredeterminada();
        spokeDuringVisit = true;
    }

    private bool IsCalm()
    {
        if (health != null && health.IsDead) return false;
        if (AlarmManager.Instance != null && AlarmManager.Instance.IsAlarmActive) return false;
        if (suspicion != null && (suspicion.CurrentSuspicion > 0f || suspicion.IsInvestigating)) return false;

        if (npc == null) return true;
        NPCState state = npc.GetCurrentState();
        return state == NPCState.Idle || state == NPCState.Patrol;
    }

    private Transform FindHead()
    {
        NPCAssembler assembler = GetComponent<NPCAssembler>();
        if (assembler != null && assembler.humanoid != null)
        {
            Animator baseAnimator = assembler.humanoid.GetComponentInChildren<Animator>(true);
            Transform baseHead = GetAnimatorHead(baseAnimator);
            if (baseHead != null) return baseHead;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            Transform head = GetAnimatorHead(animator);
            if (head != null) return head;
        }

        return FindChild(transform, "mixamorig:Head") ?? transform;
    }

    private static Transform GetAnimatorHead(Animator animator)
    {
        return animator != null && animator.avatar != null && animator.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.Head)
            : null;
    }

    private GameObject FindExistingCanvas()
    {
        foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.name.StartsWith("Dialogo_TextoFlotante")) return canvas.gameObject;
        }

        return null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child;
        }

        return null;
    }
}
