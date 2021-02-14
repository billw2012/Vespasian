using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarActiveComponent : MonoBehaviour, IUpgradeLogic
{
    // -------- Radar settings ----------
    [SerializeField]
    private float maxPingRange = 750;

    [SerializeField]
    private float pingRechargeDuration = 3.0f;

    // --------------------------------------



    // ----------------- External components ----------------------

    // Audio sources
    [SerializeField]
    private AudioSource audioSource = null;
    [SerializeField]
    private AudioClip audioClipPing = null;
    [SerializeField]
    private AudioClip audioClipReflection = null;

    // Prefab of ping which has RadarActovePingComponent
    [SerializeField]
    private GameObject pingPrefab = null;

    // ---------------------------------------------------------



    // ----------------- Public properties --------------

    // Returns true when we can Ping() again
    public bool ReadyToPing { get; private set; }

    // Returns value 0..1 showing how charged the radar is
    // Meant to be used for indicators
    // 1 means it's ready to Ping()
    public float RechargeProgress
    {
        get => this.ReadyToPing ? 1.0f : this.pingRechargeTimer / this.pingRechargeDuration;
    }

    // -------------------------------------



    // --------------- Internals ----------------

    private Simulation simulation;

    // Reflector of this ship
    private RadarActiveReflector reflector = null;

    private RadarActiveIndicatorManagerComponent indicatorMgr = null;

    private float pingRechargeTimer = 0;

    private RadarActivePingComponent lastPing = null;

    // -------------------------------------------



    // ------------- IUpgradeDef interface ---------------------
    public UpgradeDef upgradeDef { get; private set; }

    void IUpgradeLogic.Install(UpgradeDef upgradeDef)
    {
        this.upgradeDef = upgradeDef;
    }

    void IUpgradeLogic.TestFire()
    {
        
    }

    void IUpgradeLogic.Uninstall()
    {
        
    }

    // -------------------------------------------------------


    // Start is called before the first frame update
    void Awake()
    {
        this.ReadyToPing = true;
        this.simulation = ComponentCache.FindObjectOfType<Simulation>();
        this.reflector = this.GetComponentInParent<RadarActiveReflector>();
        this.indicatorMgr = ComponentCache.FindObjectOfType<RadarActiveIndicatorManagerComponent>();
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTimeReal = Time.deltaTime * this.simulation.tickStep;

        // Handle recharging
        if (!this.ReadyToPing)
        {
            this.pingRechargeTimer += deltaTimeReal;
            if (this.pingRechargeTimer >= this.pingRechargeDuration)
            {
                this.pingRechargeTimer = 0;
                this.ReadyToPing = true;
            }
        }

        // for testing
        //if (this.ReadyToPing)
        //{
        //    this.Ping();
        //}
    }

    public void Ping()
    {
        //if (!this.ReadyToPing)
        //    return; // Need to recharge first!

        //Debug.Log("Radar Ping");

        // Delete previous ping
        if (this.lastPing != null)
        {
            Destroy(this.lastPing.gameObject);
        }
        this.lastPing = null;

        // Create the ping object
        var pingGameObj = GameObject.Instantiate(this.pingPrefab);
        var pingComponent = pingGameObj.GetComponent<RadarActivePingComponent>();
        pingComponent.InitPing(this, this.maxPingRange, this.transform.position);
        this.lastPing = pingComponent;

        this.audioSource.PlayOneShot(this.audioClipPing, 1.0f);

        this.ReadyToPing = false;
    }

    public void OnPingReceived(RadarActiveReflector reflector)
    {
        // Ignore received signel from own ship
        if (reflector != this.reflector)
        {
            Debug.Log($"{this} has detected reflection from {reflector}");
            this.audioSource.PlayOneShot(this.audioClipReflection, 1.0f);
            this.indicatorMgr.CreateIndicator(reflector.transform.position, reflector.Velocity);
        }
    }
}
