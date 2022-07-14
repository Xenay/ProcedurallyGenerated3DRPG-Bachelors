using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject Axe;
    public float dmg = 5.5f;
    public bool CanAttack = true;
    public float AttackCooldown = 1.0f;
    public bool IsAttacking = false;
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if(CanAttack)
            {
                SwordAttack();
            }
        }
    }

    public void SwordAttack()
    {
        IsAttacking = true;
        CanAttack = false;
        Animator anim = Axe.GetComponent<Animator>();
        anim.SetTrigger("Attack");
        StartCoroutine(ResetAttackCooldown());
    }

    public IEnumerator ResetAttackCooldown()
    {
        StartCoroutine(ResetAttackBool());
        yield return new WaitForSeconds(AttackCooldown);
        CanAttack = true;
    }
    IEnumerator ResetAttackBool()
    {
        yield return new WaitForSeconds(1.0f);
        IsAttacking = false;
    }
}
