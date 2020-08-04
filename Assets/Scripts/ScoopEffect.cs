public class ScoopEffect : RadiusEffect
{
    protected override void Apply(float value)
    {
        GameLogic.Instance.AddFuel(value);
    }
};
