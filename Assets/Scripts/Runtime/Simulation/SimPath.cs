// unset

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class SimPath
{
    public PathSection pathSection;
    public Dictionary<GravitySource, PathSection> relativePaths;
    public List<SimModel.SphereOfInfluence> sois;
    public int crashTick = -1;
    public Vector3 crashPosition;
    public bool willCrash => this.crashTick != -1;
    
    public bool HaveCrashed(int simTick) => this.willCrash && simTick >= this.crashTick;

    public void TrimStart(int beforeTick)
    {
        this.pathSection.TrimStart(beforeTick);
        foreach(var r in this.relativePaths.Values)
        {
            r.TrimStart(beforeTick);
        }
        this.sois = this.sois.Where(s => s.endTick > this.pathSection.startTick).ToList();
        // this.sois.FirstOrDefault()?.relativePath.TrimStart(beforeTick);
    }

    public void Append(SimPath other)
    {
        Assert.IsFalse(this.willCrash);

        this.pathSection.Append(other.pathSection);
        foreach(var gp in other.relativePaths)
        {
            this.relativePaths[gp.Key].Append(gp.Value);
        }

        this.crashTick = other.crashTick;
        this.crashPosition = other.crashPosition;

        // If we have sois to merge
        if (this.sois.Any() && other.sois.Any() && this.sois.Last().g == other.sois.First().g)
        {
            var ourLastSoi = this.sois.Last();
            var otherFirstSoi = other.sois.First();
            if (otherFirstSoi.maxForce > ourLastSoi.maxForce)
            {
                ourLastSoi.maxForce = otherFirstSoi.maxForce;
                ourLastSoi.maxForcePosition = otherFirstSoi.maxForcePosition;
                ourLastSoi.maxForceTick = otherFirstSoi.maxForceTick;
            }
            ourLastSoi.endTick = otherFirstSoi.endTick;
            // ourLastSoi.relativePath.Append(otherFirstSoi.relativePath);
            // We merged the first soi of the others so we skip it and append the remaining ones
            this.sois.AddRange(other.sois.Skip(1));
        }
        else
        {
            // This covers all other cases
            this.sois.AddRange(other.sois);
        }
    }
}