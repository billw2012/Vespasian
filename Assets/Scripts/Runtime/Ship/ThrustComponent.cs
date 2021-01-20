// unset

using UnityEngine;

public class ThrustComponent : MonoBehaviour, IUpgradeLogic
{
    public float forwardThrust = 1;
    public float reverseThrust = 1;
    public float lateralThrust = 1;
    
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void TestFire() {}
    public void Uninstall() {}
}
