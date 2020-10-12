using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MultipleAsteroids : MonoBehaviour, ISimUpdate
{
    public GameObject asteroidPrefab;

    [Range(1, 20)]
    public int count = 1;
    [Range(0, 20)]
    public float radiusRandomization = 0.5f;
    [Range(0, 1)]
    public float orbitRandomization = 0.2f;

    struct Asteroid
    {
        public GameObject obj;
        public float radiusOffset;
        public float timeOffset;
    }
    List<Asteroid> asteroids;

    Orbit orbit;

    SimManager simManager;

    void Awake()
    {
        this.simManager = FindObjectOfType<SimManager>();
    }

    #region ISimUpdate
    public void SimUpdate(int _)
    {
        this.DelayedStart();
        this.UpdatePositions();
    }

    public void SimRefresh() { }
    #endregion

    void FixedUpdate()
    {
        if (this.simManager == null)
        {
            this.DelayedStart();
            this.UpdatePositions();
        }
    }

    void UpdatePositions()
    {
        float t = this.simManager == null ? Time.time : this.simManager.time;

        // Asteroids can be destroyed, so check for null
        foreach (var asteroid in this.asteroids.Where(a => a.obj != null))
        {
            var pos = this.orbit.orbitPath.GetPosition(t + asteroid.timeOffset);
            asteroid.obj.transform.localPosition = pos + pos.normalized * asteroid.radiusOffset;
        }
    }

    void DelayedStart()
    {
        if (this.orbit != null)
            return;

        this.orbit = this.GetComponent<Orbit>();
        this.asteroids = new List<Asteroid>();

        float period = this.orbit.orbitPath.period;
        for (int i = 0; i < this.count; i++)
        {
            this.asteroids.Add(new Asteroid
            {
                obj = Instantiate(this.asteroidPrefab, this.transform),
                radiusOffset = Random.Range(-1f, 1f) * this.radiusRandomization,
                timeOffset = (i + Random.Range(-0.5f, 0.5f) * this.orbitRandomization) * period / this.count
            });
        }

        this.UpdatePositions();
    }
}
