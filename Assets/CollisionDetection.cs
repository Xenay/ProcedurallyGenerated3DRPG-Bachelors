using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    public WeaponController wc;
    public GameObject HitParticle;

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy" && wc.IsAttacking)
        {
            other.GetComponent<Animator>().SetTrigger("GetHit");
            Instantiate(HitParticle, new Vector3(other.transform.position.x, transform.position.y, other.transform.position.z), other.transform.rotation);

            other.GetComponent<EnemyAi>().health-=wc.dmg;
            StartCoroutine(wc.ResetAttackCooldown());

            if (other.GetComponent<EnemyAi>().health <= 0)
            {
                GetComponent<Animator>().SetTrigger("Die");
                StartCoroutine(DecreaseHealth());
                Destroy(other.gameObject);
            }
        }
    }
    public IEnumerator DecreaseHealth()
    {
       
        yield return new WaitForSeconds(2.0f);
    }
}
