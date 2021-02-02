// unset

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class PathSection
{
    // private struct Key
    // {
    //     public int tick;
    //     public Vector3 position;
    // }
    public readonly List<Vector3> positions;
    public readonly List<Vector3> velocities;
    public readonly int tickStep;
    public int startTick;

    // A single position has duration of 0, so we adjust positions.Count here to reflect that
    public int durationTicks => Mathf.Max(0, this.positions.Count - 1) * this.tickStep;
    public int endTick => this.startTick + this.durationTicks;
    public Vector3 finalVelocity => this.velocities[this.velocities.Count - 1];
    public Vector3 finalPosition => this.positions[this.positions.Count - 1];

    // We are always "in range" if this path section predicts a crash 
    public bool InRange(int tick) => tick >= this.startTick && tick <= this.endTick;
    // public bool HaveCrashed(int tick) => this.willCrash && tick >= this.endTick;

    public PathSection(int startTick, int tickStep)
    {
        Assert.IsTrue(Mathf.IsPowerOfTwo(tickStep));
        this.startTick = startTick;
        this.tickStep = tickStep;
        this.positions = new List<Vector3>();
        this.velocities = new List<Vector3>();
    }

    public (Vector3, Vector3) GetPositionVelocity(int tick, float dt)
    {
        Assert.IsTrue(this.InRange(tick));

        float fIdx = (tick - this.startTick) / (float)this.tickStep;

        int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, this.positions.Count - 1);
        int idx1 = Mathf.Clamp(idx0 + 1, 0, this.positions.Count - 1);
        float t = fIdx - Mathf.FloorToInt(fIdx);
        return (
            Vector3.Lerp(this.positions[idx0], this.positions[idx1], t).xy0(),
            Vector3.Lerp(this.velocities[idx0], this.velocities[idx1], t).xy0()
        );
    }
    
    public (Vector3 position, Vector3 velocity) GetPositionVelocityHermite(int tick, float dt)
    {
        Assert.IsTrue(this.InRange(tick));

        float fIdx = (tick - this.startTick) / (float)this.tickStep;

        int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, this.positions.Count - 1);
        int idx1 = Mathf.Clamp(idx0 + 1, 0, this.positions.Count - 1);

        float t = fIdx - Mathf.FloorToInt(fIdx);
        return (
            MathX.Hermite(
                this.positions[idx0],
                this.velocities[idx0] * (this.tickStep * dt), 
                this.positions[idx1],
                this.velocities[idx1] * (this.tickStep * dt),
                t).xy0(),
            Vector3.Lerp(this.velocities[idx0], this.velocities[idx1], t).xy0()
        );
    }
    
    public void Add(Vector3 pos, Vector3 velocity)
    {
        this.positions.Add(pos.xy0());
        this.velocities.Add(velocity.xy0());
    }
    
    public void TrimStart(int beforeTick)
    {
        int count = Mathf.Clamp((beforeTick - this.startTick) / this.tickStep, 0, this.positions.Count);
        this.positions.RemoveRange(0, count);
        this.velocities.RemoveRange(0, count);        
        this.startTick += count * this.tickStep;
    }

    public void Append(PathSection other)
    {
        Assert.AreEqual(other.startTick, this.endTick, "Next section start should exactly match current section end");
        Assert.AreEqual(other.tickStep, this.tickStep, "All sections must have the same tick step");
        Assert.AreEqual(other.positions.First(), this.positions.Last(), "Next section start should exactly match current section end");
        Assert.AreEqual(other.velocities.First(), this.velocities.Last(), "Next section start should exactly match current section end");
        
        // Skip the first position as it will be identical
        this.positions.AddRange(other.positions.Skip(1));
        this.velocities.AddRange(other.velocities.Skip(1));
    }
}