public class StarOrPlanetGenerator : BodyGenerator
{
    protected StarOrPlanet starOrPlanet => this.body as StarOrPlanet;

    protected override void InitInternal(RandomX rng)
    {
        base.InitInternal(rng);

        // Extend radius effect range by our radius (-1 because that is the default radius that is already considered in the effect range)
        foreach(var effect in this.GetComponents<EffectSource>())
        {
            effect.range += this.starOrPlanet.radius - 1;
        }

        var scannable = this.GetComponent<Scannable>();
        if (scannable != null)
        {
            this.GetComponent<Scannable>().scannedObjectRadius = this.starOrPlanet.radius;
        }
    }
}
