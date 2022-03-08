using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ExpPickup : Pickup
{
    public Material defaultMaterial;
    public Material superMaterial;
    [ReadOnly] public Player target;
    private SpriteRenderer _sr;
    public int expAmount;
    public Sprite small, medium, large;
    public override void Initialize()
    {
        _sr = GetComponent<SpriteRenderer>();

        GetComponent<CircleCollider2D>().radius = GameManager.Instance.PickupRadius + 1f;

        //Collider2D[] expDrops = Physics2D.OverlapCircleAll(transform.position, GameManager.Instance.PickupRadius + 5f, LayerMask.GetMask("Experience"));
        //if(expDrops.Length > 20)
        //{
        //    for (int i = 0; i < expDrops.Length; i++)
        //    {
        //        this.expAmount += expDrops[i].GetComponent<ExpPickup>().expAmount;
        //        expDrops[i].gameObject.SetActive(false);
        //    }
        //}

        if (expAmount == 1)
        {
            _sr.sprite = small;
            _sr.material = defaultMaterial;
        }
        else if (expAmount <= 10)
        {
            _sr.sprite = medium;
            _sr.material = defaultMaterial;
        }
        else if (expAmount <= 100)
        {
            _sr.sprite = large;
            _sr.material = defaultMaterial;
        }
        else if (expAmount <= 1000)
        {
            _sr.sprite = small;
            _sr.material = superMaterial;
        }
        else if (expAmount <= 10000)
        {
            _sr.sprite = medium;
            _sr.material = superMaterial;
        }
        else
        {
            _sr.sprite = large;
            _sr.material = superMaterial;
        }
    }
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            target = collision.GetComponent<Player>();
        }
    }
    private void FixedUpdate()
    {
        if (target != null)
        {
            transform.Translate((target.transform.position - transform.position).normalized * Time.fixedDeltaTime * 3f);

            if (Vector2.Distance(target.transform.position, transform.position) < 0.5f)
            {
                OnPickup();
            }
        }
    }
    private void OnDisable()
    {
        target = null;
    }
    public override void OnPickup()
    {
        Debug.Log("Picking up " + expAmount);
        gameObject.SetActive(false);
        GameManager.Instance.PlayerExperience += expAmount;
    }
}