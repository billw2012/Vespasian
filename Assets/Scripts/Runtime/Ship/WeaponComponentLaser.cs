using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;

/// <summary>
/// Instant medium range, medium damage, weapon.
/// </summary>
public class WeaponComponentLaser : WeaponComponentBase
{
    
    [SerializeField]
    private float damagePerSecond = 0.1f;
    [SerializeField]
    private ParticleSystem laserEffect = null;
    [SerializeField]
    private float laserRange = 5.0f;

    protected override void BeforeLateUpdate()
    {
        // We disable laser here, but it's re-enabled later if we keep firing
        this.laserEffect.gameObject.SetActive(false);
    }

    protected override void FireInternal(Vector3 fireDir) {
        // Perform ray cast, select nearest hit
        GameObject thisGameObject = this.gameObject;
        RaycastHit[] intersects = Physics.RaycastAll(this.transform.position, fireDir, this.laserRange);
        Vector3 thisPos = this.transform.position;
        var intersectionsSorted = intersects.Where(i => i.collider.gameObject != thisGameObject)
            .Select(i => (collider: i.collider, dist: Vector3.Distance(thisPos, i.point), pos: i.point))
            .OrderBy(i => i.dist)
            .ToArray();
        bool hitTarget = intersectionsSorted.Length > 0;
        Vector3 impactPos = hitTarget ?   // Calculate impact position
            intersectionsSorted[0].pos :
            thisPos + this.laserRange * fireDir;

        // Handle the laser effect
        this.laserEffect.gameObject.SetActive(true);
        var vectorToTarget = impactPos - thisPos;
        this.laserEffect.transform.rotation =
            Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget));
        this.laserEffect.transform.localScale = new Vector3(vectorToTarget.magnitude, 1, 1);

        if (hitTarget)
        {
            // Add damage
            var healthComponent = intersectionsSorted[0].collider.GetComponentInParent<HealthComponent>();
            if (healthComponent != null)
                healthComponent.AddDamage(this.damagePerSecond * Time.deltaTime, fireDir);
        }
    }

    /*
    private float firingCycleRemaining;
    private ControllerBase target;
    private float firingCycleFraction => 1f - this.firingCycleRemaining / (base.firingCooldownTime * 0.5f);
    private Vector3 finalOffset;
    
    protected override void Fire(ControllerBase target)
    {
        this.firingCycleRemaining = base.firingCooldownTime * 0.5f;
        this.target = target;
        this.finalOffset = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360)) * Vector3.right * Random.Range(0f, 2f);
    }
    */    
}