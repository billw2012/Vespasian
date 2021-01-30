using UnityEngine;

public class Sensors : MonoBehaviour, IUpgradeLogic
{
    public float range = 20f;

    // Update is called once per frame
    private void Update()
    {
        // Reveal effects
        var detectedEffects = EffectSource.AllInDetectionRange<EffectSource>(this.transform, this.range);
        foreach (var source in detectedEffects)
        {
            source.Reveal();
        }
    }

    #region IUpgradeLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void Uninstall() { }
    public void TestFire() { }
    #endregion IUpgradeLogic
}
