using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpController : MonoBehaviour
{
    // Start is called before the first frame update
    public WeaponController weapon;
    public Rigidbody rb;
    public BoxCollider coll;
    public Transform player,weaponHolder,cameraPlayer;

    public float pickUpRange;
    public float dropForwardForce, dropUpwardForce;

    public bool equipped;
    public static bool slotFull;

    private void Start()
    {
        if(!equipped)
        {
            rb.isKinematic = false;
            coll.isTrigger = false;

        }
        if (equipped)
        {
            rb.isKinematic = true;
            coll.isTrigger = true;
            slotFull = true;

        }
    }
    private void Update()
    {
        Vector3 distanceToPlayer = player.position - transform.position;
        if (equipped && distanceToPlayer.magnitude <= pickUpRange && Input.GetKeyDown(KeyCode.E) && !slotFull) Pickup();
        if (equipped && Input.GetKeyDown(KeyCode.Q)) Drop();
    }
    private void Pickup()
    {
        equipped = true;
        slotFull = true;

        transform.SetParent(weaponHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        transform.localScale = Vector3.one;

        rb.isKinematic = true;
        coll.isTrigger = true;


    }
    private void Drop()
    {
        equipped = false;
        slotFull = false;

        transform.SetParent(null);

        rb.isKinematic = false;
        coll.isTrigger = false;

        rb.velocity = player.GetComponent<Rigidbody>().velocity;

        rb.AddForce(cameraPlayer.forward * dropForwardForce, ForceMode.Impulse);
        rb.AddForce(cameraPlayer.up * dropUpwardForce, ForceMode.Impulse);
        float random = Random.Range(-1f, 1f);
        rb.AddTorque(new Vector3(random, random, random) * 10);
    }
}
