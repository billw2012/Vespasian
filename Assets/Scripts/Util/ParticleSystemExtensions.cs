using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Modifying a particle system at runtime involves changing values on the 
// structs it contains, however you can't modify a struct via a property accessor,
// so instead you have to copy it, then modify it.
// This is some kludge due to the struct actually being an interface to a native object.
public static class ParticleSystemExtensions
{
    public static void SetEmissionEnabled(this ParticleSystem pfx, bool enabled)
    {
        var em = pfx.emission;
        if (em.enabled != enabled)
        {
            em.enabled = enabled;
        }
    }

    public static void SetEmissionRateOverTimeMultiplier(this ParticleSystem pfx, float rateOverTimeMultiplier)
    {
        var em = pfx.emission;
        em.rateOverTimeMultiplier = rateOverTimeMultiplier;
    }
}