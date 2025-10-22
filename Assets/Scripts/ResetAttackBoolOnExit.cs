using UnityEngine;

public class ResetAttackBoolOnExit : StateMachineBehaviour
{
    public string boolParameterName; // Inspector'dan "sword_outward_slash" olarak ayarlanacak

    // ResetAttackBoolOnExit.cs içine ekleyin
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"OnStateExit çaðrýldý. State: {stateInfo.shortNameHash}, Parametre Adý: {boolParameterName}"); // Hangi state'den çýktýðýný ve parametre adýný logla

        if (!string.IsNullOrEmpty(boolParameterName))
        {
            bool currentValue = animator.GetBool(boolParameterName); // Mevcut deðeri logla
            Debug.Log($"{boolParameterName} parametresinin mevcut deðeri: {currentValue}");

            animator.SetBool(boolParameterName, false);
            Debug.Log($"{boolParameterName} parametresi false olarak ayarlandý.");
        }
        else
        {
            Debug.LogWarning("ResetAttackBoolOnExit: boolParameterName Inspector'dan ayarlanmamýþ!");
        }
    }
}