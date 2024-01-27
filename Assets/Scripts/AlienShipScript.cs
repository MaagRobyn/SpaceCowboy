using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlienShipScript : ShipScript
{
    [SerializeField] private GameObject Player;
    [SerializeField] private ScriptableAlien ship;
    private int dangerLevel;
    private const int LERPFACTOR = 2000;
    private Vector3 destinationCoordinates;

    private FlightPattern flightPattern;
    enum FlightPattern
    {
        Wander,
        Chase,
        Cruise,
        Drone
    }

    // Start is called before the first frame update
    void Start()
    {
        speed = ship.speed;
        for(int i = 0; i < cannons.Count && i < ship.cannons.Count; i++)
        {
            cannons[i].cannon = ship.cannons[i];
            
        }
        health = ship.health;
        dangerLevel = ship.dangerLevel;
        shipProjectileDamageMod = ship.damageMod;
        shipProjectileDamageMult = ship.damageMult;
        shipProjectileVelocityMod = ship.velocityMod;
        shipProjectileVelocityMult = ship.velocityMult;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = ship.sprite;

        flightPattern = FlightPattern.Chase;
        GameManager.Instance.Player.OnPlayerDeath += () =>
        {
            flightPattern = FlightPattern.Wander;
        };
    }

    // Update is called once per frame
    void Update()
    {
        FireCannons();
    }

    private void FixedUpdate()
    {
        switch (flightPattern)
        {
            case FlightPattern.Chase:
                destinationCoordinates = GameManager.Instance.Player.transform.position;
                if (Vector3.Distance(transform.position, destinationCoordinates) <= 1)
                {
                    return;
                }
                break;
            case FlightPattern.Wander:
                if (Vector3.Distance(transform.position, destinationCoordinates) <= 1)
                {
                    var x = UnityEngine.Random.Range(-1000, 1000);
                    var y = UnityEngine.Random.Range(-1000, 1000);
                    var z = UnityEngine.Random.Range(-1000, 1000);
                    destinationCoordinates = new Vector3(x, y, z);
                }
                break;
            case FlightPattern.Cruise:
                rb.AddForce(Tools.GetUnitVector2(rb.rotation));
                return;
        }
        GoToTarget(destinationCoordinates);
        RotateTowardsTarget(destinationCoordinates);

    }

    private void RotateTowardsTarget(Vector3 target)
    {
        float angle = Tools.FindAngleBetweenTwoPositions(transform.position, target);
        rb.SetRotation(angle);
    }

    private void GoToTarget(Vector3 target)
    {
        rb.position = Vector2.Lerp(transform.position, target, speed / LERPFACTOR);
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public int GetDangerLevel()
    {
        return dangerLevel;
    }
}
