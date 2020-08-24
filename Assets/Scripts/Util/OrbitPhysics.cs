using UnityEngine;

// http://www.braeunig.us/space/orbmech.htm
// https://evgenii.com/blog/earth-orbit-simulation/
public class OrbitPhysics
{
    struct Xdx
    {
        public float X;
        public float dx;
    };

    Xdx distanceXdx;
    Xdx angleXdx;

    readonly float mass;
    readonly float gravitationalConstant;

    // http://www.braeunig.us/space/orbmech.htm 4.6
    public static float CircularOrbitalVelocity(float radius, float mass, float G)
    {
        return Mathf.Sqrt(G * mass / radius);
    }


    // http://www.braeunig.us/space/orbmech.htm 4.8
    public static float CircularOrbitalAngularVelocity(float radius, float mass, float G)
    {
        return Mathf.Rad2Deg * G * mass / Mathf.Pow(radius, 3);
    }

    // http://www.braeunig.us/space/orbmech.htm 4.9
    public static float OrbitalPeriod(float semiMajorAxis, float mass, float G)
    {
        return Mathf.Sqrt(4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(semiMajorAxis, 3) / G * mass);
    }

    public static float OrbitalVelocityToAngularVelocity(float radius, float v)
    {
        return Mathf.Rad2Deg * 2f * Mathf.PI * Mathf.PI * Mathf.Pow(radius, 2) / v;
    }

    // http://www.braeunig.us/space/orbmech.htm 4.16 4.17
    // Vp: velocity at periapsis
    // Rp (periapsis): closest distance of orbit
    // Ra (apoapsis): furthest distance of orbit
    public static float Vp(float Rp, float Ra, float M, float G)
    {
        return Mathf.Sqrt(2 * G * M * Ra / (Rp * (Ra + Rp)));
    }

    //public static float EccentricOrbitalVelocity(float radius, float mass, float G, float eccentricity)
    //{
    //    // ??return Mathf.Sqrt(G * mass / radius);
    //}

    public Vector2 GetPosition() => new Vector2(Mathf.Cos(this.angleXdx.X), Mathf.Sin(this.angleXdx.X)) * this.distanceXdx.X;
    public float distance => this.distanceXdx.X;
    public float angle => this.angleXdx.X * Mathf.Rad2Deg;

    public OrbitPhysics(float distance, float angle, float angularVelocity, float mass, float gravitationalConstant)
    {
        this.distanceXdx = new Xdx { X = distance, dx = 0 };
        this.angleXdx = new Xdx { X = angle * Mathf.Deg2Rad, dx = angularVelocity * Mathf.Deg2Rad };
        this.mass = mass;
        this.gravitationalConstant = gravitationalConstant;
    }

    // Calculates position of the Earth
    public void Step(float dt)
    {
        float CalculateDistanceAcceleration()
        {
            // [acceleration of distance] = [distance][angular velocity]^2 - G * M / [distance]^2
            return this.distanceXdx.X * Mathf.Pow(this.angleXdx.dx, 2) - this.gravitationalConstant * this.mass / Mathf.Pow(this.distanceXdx.X, 2);
        }

        float CalculateAngleAcceleration()
        {
            // [acceleration of angle] = - 2[speed][angular velocity] / [distance]
            return -2f * this.distanceXdx.dx * this.angleXdx.dx / this.distanceXdx.X;
        }

        // Calculates a new value based on the time change and its derivative
        // For example, it calculates the new distance based on the distance derivative (velocity)
        // and the elapsed time interval.
        float AdvanceValue(float currentValue, float derivative)
        {
            return currentValue + dt * derivative;
        }

        // Calculate new distance
        float distanceAcceleration = CalculateDistanceAcceleration();
        this.distanceXdx.dx = AdvanceValue(this.distanceXdx.dx, distanceAcceleration);
        this.distanceXdx.X = AdvanceValue(this.distanceXdx.X, this.distanceXdx.dx);

        // Calculate new angle
        float angleAcceleration = CalculateAngleAcceleration();
        this.angleXdx.dx = AdvanceValue(this.angleXdx.dx, angleAcceleration);
        this.angleXdx.X = AdvanceValue(this.angleXdx.X, this.angleXdx.dx);

        //this.angleXdx.X %= 2 * Mathf.PI;
    }
}
