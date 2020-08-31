using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public static class OrbitalUtils
{
    public static Vector3 CalculateForce(Vector3 vec, float M, float G)
    {
        return vec.normalized * G * M / Mathf.Pow(vec.magnitude, 2);
    }

    public static float SemiMajorAxis(float periapsis, float apoapsis)
    {
        return (periapsis + apoapsis) / 2f;
    }

    // http://www.braeunig.us/space/orbmech.htm 4.9
    public static float OrbitalPeriod(float semiMajorAxis, float mass, float G)
    {
        return Mathf.Sqrt(4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(semiMajorAxis, 3) / (G * mass));
    }

    public static float OrbitalVelocityToAngularVelocity(float radius, float v)
    {
        return v / radius;
    }

    // http://www.braeunig.us/space/orbmech.htm 4.16 4.17
    // Vp: velocity at periapsis
    // Rp (periapsis): closest distance of orbit
    // Ra (apoapsis): furthest distance of orbit
    public static float Vp(float Rp, float Ra, float M, float G)
    {
        return Mathf.Sqrt(2f * G * M * Ra / (Rp * (Ra + Rp)));
    }
}

// Ordinary differential equation solver
public static class ODE
{
    public static void RungeKutta(float h, float[] u, Func<float[], float[]> derivative)
    {
        int dimension = u.Length;

        // TODO optimize with stack alloc, or make the class an instance and make these member variables
        float[] a = new[] { h / 2, h / 2, h, 0 };
        float[] b = new[] { h / 6, h / 3, h / 3, h / 6 };
        float[] u0 = (float[])u.Clone();
        float[] ut = new float[dimension];

        for (int j = 0; j < 4; j++)
        {
            float[] du = derivative(u);

            for (int i = 0; i < dimension; i++)
            {
                u[i] = u0[i] + a[j] * du[i];
                ut[i] = ut[i] + b[j] * du[i];
            }

            if(u.Any(x => float.IsNaN(x)))
            {
                Assert.IsFalse(u.Any(x => float.IsNaN(x)));
            }
        }

        for (int i = 0; i < dimension; i++)
        {
            u[i] = u0[i] + ut[i];
        }
    }
}

// http://www.braeunig.us/space/orbmech.htm
// https://evgenii.com/blog/earth-orbit-simulation/
public class OrbitPhysics
{
    readonly float periapsis; // nearest distance to primary
    readonly float apoapsis; // farthest distance to primary
    readonly float mass; // mass of primary
    readonly float G; // gravitational constant

    float semiMajorAxis => OrbitalUtils.SemiMajorAxis(this.periapsis, this.apoapsis);
    public float period => OrbitalUtils.OrbitalPeriod(this.semiMajorAxis, this.mass, this.G);

    public bool valid => this.periapsis > 0 && this.apoapsis >= this.periapsis && this.mass > 0 && this.G > 0;

    public Vector2 GetPosition() => new Vector2(Mathf.Cos(this.u[Angle]), Mathf.Sin(this.u[Angle])) * this.distance;

    public float distance => this.u[Distance];
    public float angle => this.u[Angle] * Mathf.Rad2Deg;
    public float da => this.u[AngularVelocity];

    const int Angle = 0;
    const int Distance = 1;
    const int AngularVelocity = 2;
    const int Speed = 3;

    // Variables to be integrated, representing current position in orbit, and its derivative
    readonly float[] u;

    public OrbitPhysics(float periapsis, float apoapsis, float angle, float mass, float G)
    {
        this.periapsis = periapsis;
        this.apoapsis = apoapsis;
        this.mass = mass;
        this.G = G;

        float angularVelocityAtPeriapsis = OrbitalUtils.OrbitalVelocityToAngularVelocity(periapsis, OrbitalUtils.Vp(periapsis, apoapsis, mass, G));

        this.u = new float[] { angle * Mathf.Deg2Rad, periapsis, angularVelocityAtPeriapsis, 0 };
    }

    // Calculates position of the Earth
    public void Step(float dt)
    {
        float[] Derivatives(float[] x)
        {
            float[] dx = new float[4];
            dx[Angle] = x[AngularVelocity]; // derivative of angle is angular velocity
            dx[Distance] = x[Speed]; // derivative of distance is speed
            dx[AngularVelocity] = - 2f * x[Speed] * x[AngularVelocity] / x[Distance];
            dx[Speed] = x[Distance] * Mathf.Pow(x[AngularVelocity], 2) - this.G * this.mass / Mathf.Pow(x[Distance], 2);
            return dx;
        }

        ODE.RungeKutta(dt, this.u, Derivatives);
    }
}
