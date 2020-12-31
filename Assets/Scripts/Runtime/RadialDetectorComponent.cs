// unset

using System;
using System.Linq;
using UnityEngine;

public class RadialDetectorComponent : MonoBehaviour, IUpgradeLogic
{
    [SerializeField]
    private RadialDetectorVisualComponent visual;

    public float pingInterval = 10;
    
    private float nextPing = 0;

    private void Update()
    {
        if (Time.time > this.nextPing)
        {
            this.visual.UpdateDetections(GravitySource.All().Select(g => ((Vector2)g.position, g.parameters.mass)));
            
            this.nextPing += this.pingInterval;
        }
    }

    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public async void TestFire() {}
    public void Uninstall() {}
}