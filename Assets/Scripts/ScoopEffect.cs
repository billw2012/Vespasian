using UnityEngine;

public class ScoopEffect : RadiusEffect
{
    protected override void Apply(float value, Vector3 direction)
    {
        GameLogic.Instance.AddFuel(value);
    }
};
