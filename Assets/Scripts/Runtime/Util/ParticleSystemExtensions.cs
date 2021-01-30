using System;
using UnityEngine;

// Modifying a particle system at runtime involves changing values on the 
// structs it contains, however you can't modify a struct via a property accessor,
// so instead you have to copy it, then modify it.
// This is some kludge due to the struct actually being an interface to a native object.
public static class ParticleSystemExtensions
{
    public static void SetMainValues(this ParticleSystem pfx, Action<ParticleSystem.MainModule> fn)
    {
        fn(pfx.main);
    }

    public static void SetEmissionValues(this ParticleSystem pfx, Action<ParticleSystem.EmissionModule> fn)
    {
        fn(pfx.emission);
    }

    public static void SetVelocityOverLifetimeValues(this ParticleSystem pfx, Action<ParticleSystem.VelocityOverLifetimeModule> fn)
    {
        fn(pfx.velocityOverLifetime);
    }

    public static void SetColorOverLifetimeValues(this ParticleSystem pfx, Action<ParticleSystem.ColorOverLifetimeModule> fn)
    {
        fn(pfx.colorOverLifetime);
    }

    public static void SetEmissionEnabled(this ParticleSystem pfx, bool enabled)
    {
        pfx.SetEmissionValues(em => { if (em.enabled != enabled) em.enabled = enabled; });
    }

    public static void SetEmissionRateOverTimeMultiplier(this ParticleSystem pfx, float rateOverTimeMultiplier)
    {
        pfx.SetEmissionValues(em => em.rateOverTimeMultiplier = rateOverTimeMultiplier);
    }
}