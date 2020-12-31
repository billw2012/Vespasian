// unset

using System;
using System.Linq;
using UnityEngine;

public class RadialDetectorComponent : MonoBehaviour, IUpgradeLogic
{
    [SerializeField]
    private RadialDetectorVisualComponent visual = null;
    [SerializeField]
    private float pingInterval = 10;
    [SerializeField] 
    private float massScaling = 0.1f;
    
    private float nextPing = 0;

    private void Update()
    {
        if (Time.time > this.nextPing)
        {
            this.visual.UpdateDetections(GravitySource.All().Select(g => ((Vector2)g.position, g.parameters.mass * this.massScaling)));
            
            this.nextPing += this.pingInterval;
        }
    }

    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;

    public async void TestFire()
    {
        this.enabled = false;
        await this.visual.TestFireAsync();
        this.enabled = true;
    }

    public void Uninstall() {}
}