public class DamageEffect : RadiusEffect
{
    protected override void Apply(float value)
    {
        GameLogic.Instance.AddDamage(value * 0.1f);
    }
};
