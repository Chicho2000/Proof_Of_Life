using UnityEngine;

public class EliteGuardNPC : GuardNPC
{
#if UNITY_EDITOR
    private void Reset()
    {
        // Valores por defecto sugeridos para el Inspector al añadir el componente
        attackDamage = 45;
        chaseSpeed = 5.2f;
        attackRange = 2.0f;
    }
#endif
}
