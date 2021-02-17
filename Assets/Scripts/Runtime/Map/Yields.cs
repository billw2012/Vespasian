// unset

using System;

[Serializable]
[RegisterSavableType]
public class Yields
{
    public float resource;
    public float energy;
    public float pop;

    public Yields() { }

    public Yields(float resource, float energy, float pop)
    {
        this.resource = resource;
        this.energy = energy;
        this.pop = pop;
    }
    
    public Yields((float, float, float) from)
    {
        (this.resource, this.energy, this.pop) = from;
    }
    
    public void Deconstruct(out float resource, out float energy, out float pop)
    {
        resource = this.resource;
        energy = this.energy;
        pop = this.pop;
    }

    public static Yields operator +(Yields lhs, Yields rhs) => new Yields(lhs.resource + rhs.resource, lhs.energy + rhs.energy, lhs.pop + rhs.pop);
    public static Yields operator -(Yields lhs, Yields rhs) => new Yields(lhs.resource - rhs.resource, lhs.energy - rhs.energy, lhs.pop - rhs.pop);
    public static Yields operator *(Yields lhs, float rhs) => new Yields(lhs.resource * rhs, lhs.energy * rhs, lhs.pop * rhs);
    public static Yields operator *(Yields lhs, (float, float, float) rhs) => new Yields(lhs.resource * rhs.Item1, lhs.energy * rhs.Item2, lhs.pop * rhs.Item3);
    public static Yields operator *(Yields lhs, Yields rhs) => new Yields(lhs.resource * rhs.resource, lhs.energy * rhs.energy, lhs.pop * rhs.pop);
    
    public static implicit operator Yields((float, float, float) from) => new Yields(from);
    public static implicit operator (float, float, float)(Yields from) => (from.resource, from.energy, from.pop);
    
    public override string ToString() => $"{this.resource}/{this.energy}/{this.pop}";
}