using System;
using System.Linq;
using UnityEngine;

public abstract class WeaponComponentBase : MonoBehaviour, IUpgradeLogic
{
    [SerializeField]
    protected float firingRange = 10f;
    [SerializeField]
    protected float firingCooldownTime = 3f;
    
    protected float cooldownRemaining = 0;
    
    private Faction.FactionType ownFaction;
    protected Simulation simulation;

    private void Awake()
    {
        this.ownFaction = this.GetComponentInParent<ControllerBase>().faction;
        this.simulation = FindObjectOfType<Simulation>();
    }
    
    public virtual void Update()
    {
        if (this.cooldownRemaining <= 0)
        {
            var target = FindObjectsOfType<ControllerBase>()
                .Where(c => c.faction != this.ownFaction)
                .Select(c => (d: Vector2.Distance(this.transform.position, c.transform.position), c))
                .Where(dc => dc.d < this.firingRange)
                .OrderBy(dc => dc.d)
                .Select(dc => dc.c)
                .FirstOrDefault()
            ;
            
            if (target != null)
            {
                this.Fire(target);
                this.cooldownRemaining = this.firingCooldownTime;
            }
        }
    
        this.cooldownRemaining -= Time.deltaTime * this.simulation.tickStep;
    }

    protected abstract void Fire(ControllerBase target);
    
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void TestFire() => throw new System.NotImplementedException();
    public void Uninstall() => throw new System.NotImplementedException();
}