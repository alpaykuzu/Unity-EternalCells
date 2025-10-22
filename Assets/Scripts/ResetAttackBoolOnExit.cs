using UnityEngine;

public class ResetAttackBoolOnExit : StateMachineBehaviour
{
    public string boolParameterName; // Inspector'dan "sword_outward_slash" olarak ayarlanacak

    // ResetAttackBoolOnExit.cs i�ine ekleyin
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"OnStateExit �a�r�ld�. State: {stateInfo.shortNameHash}, Parametre Ad�: {boolParameterName}"); // Hangi state'den ��kt���n� ve parametre ad�n� logla

        if (!string.IsNullOrEmpty(boolParameterName))
        {
            bool currentValue = animator.GetBool(boolParameterName); // Mevcut de�eri logla
            Debug.Log($"{boolParameterName} parametresinin mevcut de�eri: {currentValue}");

            animator.SetBool(boolParameterName, false);
            Debug.Log($"{boolParameterName} parametresi false olarak ayarland�.");
        }
        else
        {
            Debug.LogWarning("ResetAttackBoolOnExit: boolParameterName Inspector'dan ayarlanmam��!");
        }
    }
}