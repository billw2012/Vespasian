using System.Linq;
using UnityEngine;

public abstract class WeaponComponentBase : MonoBehaviour, IUpgradeLogic
{
    [SerializeField, Tooltip("Returns prefered usage range for AI")]
    public float preferredFiringRangeMin = 0;
    [SerializeField, Tooltip("Returns prefered usage range for AI")]
    public float preferredFiringRangeMax = 20f;
    
    [SerializeField, Tooltip("Heat generation per second. When heat reaches 1, it must cool down")]
    float heatGenerationRate = 0.1f;

    [SerializeField, Tooltip("Heat cooling per second. When heat reaches 0, weapon can fire again")]
    float heatCoolingRate = 0.15f;

    [SerializeField, Tooltip("Makes weapon shoot each frame and ignore reload")]
    bool shootEachFrame = false;

    [SerializeField, Tooltip("Shots per second")]
    float shotsPerSecond = 10.0f;

    [SerializeField, Tooltip("Size of magazine after which the weapon will be reloaded. Set to 0 to ignore mag logic.")]
    int magSize = 10;

    [SerializeField, Tooltip("Reload time")]
    float reloadTime = 1;

    [SerializeField, Tooltip("Start velocity to be used for firing solution calculations. Set to 0 for instant weapons (i.e. laser). It's up to the derived class how to use this variable.")]
    public float projectileStartVelocity = 0;

    [SerializeField, Tooltip("Impact damage, it's up to derived class how to use it")]
    protected float impactDamage = 0;
        
    protected Faction.FactionType ownFaction;
    protected Simulation simulation;

    private bool commandFire = false; // It is set externally if we must fire during this frame
    private bool overheat = false;
    private bool reloading = false;
    private bool waitingBetweenShots = false;
    private float heatCurrent = 0.0f;
    private int magCurrent = 1;
    private float reloadTimerCurrent = 0.0f;
    private float shootTimerCurrent = 0.0f;

    Vector3 vectorFireDir = new Vector3(1, 0, 0);


    // Public getters for some variables
    public float Heat { get { return this.heatCurrent; } }
    public bool Overheat { get { return this.overheat; } }
    public bool Reloading { get { return this.reloading; } }
    // ---------------------------------

    protected virtual void Awake()
    {
        this.ownFaction = this.GetComponentInParent<ControllerBase>().faction;
        this.simulation = FindObjectOfType<Simulation>();
        this.magCurrent = this.magSize; // We start with full mag
    }

    // We must do our things in LateUpdate, because FireAt is called in Update
    public void LateUpdate()
    {
        this.BeforeLateUpdate();

        float deltaTimeReal = Time.deltaTime * this.simulation.tickStep;
        bool didFire = false;

        // Launch projectile if we don't have overheat, we are not reloading, etc
        if (this.commandFire)
        {
            if (!this.reloading && !this.overheat && !this.waitingBetweenShots && (this.magCurrent > 0 || this.magSize == 0))
            {
                this.FireInternal(this.vectorFireDir);  // Must create the projectile object

                this.waitingBetweenShots = !this.shootEachFrame;
                this.shootTimerCurrent += 1/this.shotsPerSecond;

                this.heatCurrent += this.heatGenerationRate*deltaTimeReal;
                if (this.heatCurrent >= 1.0f)
                {
                    this.overheat = true;
                }

                this.magCurrent--;
                if (this.magCurrent == 0 && this.magSize != 0)
                {
                    this.reloading = true;
                    this.reloadTimerCurrent = this.reloadTime;
                }

                didFire = true;
            }
        }

        // Handle waiting between shots state
        if (this.waitingBetweenShots)
        {
            this.shootTimerCurrent -= deltaTimeReal;
            if (this.shootTimerCurrent <= 0)
            {
                this.waitingBetweenShots = false;
            }
        }

        // Handle reload state
        if (this.reloading)
        {
            this.reloadTimerCurrent -= deltaTimeReal;
            if (this.reloadTimerCurrent <= 0)
            {
                this.reloading = false;
                this.magCurrent = this.magSize;
            }
        }

        // Handle overheat state
        if (!didFire)
        {
            // We cool down if we didn't fire during this frame
            if (this.heatCurrent > 0)
            {
                this.heatCurrent -= this.heatCoolingRate * deltaTimeReal;
                if (this.heatCurrent < 0)
                    this.heatCurrent = 0;
            }
        }
        if (this.overheat)
        {
            if (this.heatCurrent <= 0)
            {
                this.overheat = false;
            }
        }

        // Reset the flag so we stop firing
        this.commandFire = false;
    }

    public void FireAt(GameObject target)
    {
        Vector3 offset = target.transform.position - this.transform.position;
        Vector3 offsetNorm = offset.normalized;
        this.FireAt(offsetNorm);
    }

    // This must be called from Update()!
    public void FireAt(Vector3 fireDir)
    {
        this.vectorFireDir = fireDir;
        this.commandFire = true;
    }

    // Must be implemented to perform actual firing
    // Spawn rocket
    // Spawn a bullet
    // Draw a laser beam from here to there
    protected abstract void FireInternal(Vector3 fireDir);

    // Can be implemented to perform actions before LateUpdate is called
    protected abstract void BeforeLateUpdate();

    // Helper function to instantiate a projectile
    protected GameObject InstantiateProjectile(GameObject prefab, Vector3 vectorDir, float startVelocity)
    {
        // Normalize input vectors, for safety
        vectorDir = vectorDir.normalized;
        Vector2 vectorDir2D = (Vector2)vectorDir;

        var projectile = Instantiate(prefab);
        var projectileRotation = Quaternion.FromToRotation(Vector3.up, vectorDir);
        var originSimMovement = GetComponentInParent<SimMovement>();
        var projectileSimMovement = projectile.GetComponent<SimMovement>();
        projectileSimMovement.alignToVelocity = false;
        Vector2 startVelocityVec = ((Vector2)originSimMovement.velocity) + vectorDir2D * startVelocity;
        projectileSimMovement.SetPositionVelocity(this.transform.position, projectileRotation, startVelocityVec);
        var controller = projectile.GetComponent<ControllerBase>();
        controller.faction = this.ownFaction; // Rocket faction must match faction of the ship shooting it
        return projectile;
    }

    public UpgradeDef upgradeDef { get; private set; }
    public virtual void Install(UpgradeDef upgradeDef)
    {
        this.upgradeDef = upgradeDef;

        WeaponController weapCtr = GetComponentInParent<WeaponController>();
        Debug.Assert(weapCtr != null, "Weapon controller is null!");
        if (weapCtr != null)
            weapCtr.RegisterWeapon(this);
    }

    public virtual void TestFire() {}
    public virtual void Uninstall()
    {
        WeaponController weapCtr = GetComponentInParent<WeaponController>();
        if (weapCtr != null)
            weapCtr.UnregisterWeapon(this);
    }
}