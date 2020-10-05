using GK;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

[CreateAssetMenu]
public class MapGenerator : ScriptableObject
{
    [Range(1, 200)]
    public int numberOfSystems = 50;
    //public int relaxIterations = 100;
    //public float relaxSpeed = 0.01f;

    [Range(0f, 0.75f)]
    public float minDistanceFactor = 0.5f;
    [Range(0f, 1f)]
    public float linkAcuteRemoveScore = 0.1f;

    [Range(0f, 1f)]
    public float linkObtuseRemoveScore = 0.5f;
    [Range(0f, 1f)]
    public float linkReductionRatio = 0.5f;

    [Range(0.1f, 1f)]
    public float heightRatio = 1;

    [Serializable]
    public class SystemGeneratorParameters
    {
        public float massDistributionFunctionSampleRange = 10f;
        [Tooltip("Mass distribution function median")]
        public WeightedRandom massDistributionMedianRandom = new WeightedRandom { min = 0.1f, max = 2.5f };
        [Tooltip("Mass distribution function spread")]
        public WeightedRandom massDistributionSpreadRandom = new WeightedRandom { min = 0.1f, max = 0.9f };

        [Tooltip("Star radius")]
        public WeightedRandom starRadiusRandom = new WeightedRandom { min = 5, max = 30 };
        [Tooltip("Star temperature in units of 10000 kelvin")]
        public WeightedRandom starTempRandom = new WeightedRandom { min = 0.4f, max = 5f };
        [Tooltip("Ratio of star mass to star radius")]
        public WeightedRandom starMassRadiusRatioRandom = new WeightedRandom { min = 0.5f, max = 1.5f, gaussian = true };
        [Tooltip("Ratio of system size to star mass")]
        public WeightedRandom systemSizeStarMassRatioRandom = new WeightedRandom { min = 1f, max = 15f, gaussian = true };
        [Tooltip("Ratio of system mass to star mass")]
        public WeightedRandom systemMassStarMassRatioRandom = new WeightedRandom { min = 0f, max = 1f, gaussian = true };

        public int maxPlanets = 10;
        [Tooltip("Start orbit ratio")]
        public WeightedRandom startOrbitRatioRandom = new WeightedRandom { min = 0f, max = 0.1f };
        [Tooltip("Next orbit radius base"), Range(0.5f, 3f)]
        public float nextOrbitRadiusBase = 2f;
        [Tooltip("Next orbit radius power")]
        public WeightedRandom nextOrbitRadiusPowerRandom = new WeightedRandom { min = 0.5f, max = 1.25f, gaussian = true };

        [Tooltip("How much gravitational force effects eccentricity"), Range(0, 3)]
        public float eccentricityForceFactor = 1;
        [Tooltip("How much nearness to the system rim effects eccentricity"), Range(0, 3)]
        public float eccentricityRimOrbitFactor = 1;
        [Tooltip("How eccentric an orbit can be")]
        public WeightedRandom eccentricityAmountRandom = new WeightedRandom { min = 0f, max = 1f };

        [Tooltip("How much mass varies from the exact amount described by the mass distribution function")]
        public WeightedRandom massVarianceRandom = new WeightedRandom { min = 0.75f, max = 1.25f, gaussian = true };

        [Range(0.1f, 20f)]
        public float minPlanetRadius = 3;
        [Tooltip("Ratio of planet mass to planet radius")]
        public WeightedRandom planetMassRadiusRatioRandom = new WeightedRandom { min = 0.5f, max = 1.5f, gaussian = true };
        [Tooltip("Ratio of moon system size to planet mass")]
        public WeightedRandom moonSystemSizePlanetMassRatioRandom = new WeightedRandom { min = 0.5f, max = 1.5f, gaussian = true };
        [Tooltip("Ratio of moon system mass to planet mass")]
        public WeightedRandom moonSystemMassPlanetMassRatioRandom = new WeightedRandom { min = 0f, max = 1f, gaussian = true };
    };

    public SystemGeneratorParameters systemParams;

    public SolarSystem GenerateSystem(int randomKey, BodySpecs bodySpecs, string name, Vector2 position)
    {
        Random.InitState(randomKey);

        var sys = new SolarSystem { name = name, position = position };

        float starRadius = this.systemParams.starRadiusRandom.Evaluate(Random.value);
        float starTemp = this.systemParams.starTempRandom.Evaluate(Random.value); //MathX.RandomGaussian(starTempMin, starTempMax); // 4000K to 50000K
        float starMass = starRadius * this.systemParams.starMassRadiusRatioRandom.Evaluate(Random.value);

        var mainSpec = bodySpecs.RandomStar();
        sys.main = new Body
        {
            randomKey = Random.Range(0, int.MaxValue),
            prefab = mainSpec.prefab,
            radius = starRadius,
            temp = starTemp,
            mass = starMass,
        };
        this.GenerateBodies(
            bodySpecs,
            sys.main,
            sys.main,
            systemSize: sys.main.mass * this.systemParams.systemSizeStarMassRatioRandom.Evaluate(Random.value),
            desiredTotalMass: sys.main.mass * this.systemParams.systemMassStarMassRatioRandom.Evaluate(Random.value),
            massDistributionMedian: this.systemParams.massDistributionMedianRandom.Evaluate(Random.value),
            massDistributionSpread: this.systemParams.massDistributionSpreadRandom.Evaluate(Random.value));
        return sys;
    }

    // Bodies are created by assuming a Log Normal distribution of mass wrt distance from their primary.
    // We use the CDF to slice the mass function into sections for each planet
    // See https://www.desmos.com/calculator/xfskhgkr9l
    private void GenerateBodies(BodySpecs bodySpecs, Body primaryStar, Body primary, float systemSize, float desiredTotalMass, float massDistributionMedian, float massDistributionSpread, bool allowMoons = true)
    {
        // We will sample this in a range of 0 - 10
        var massDistribution = new MathX.LogNormal(massDistributionMedian, massDistributionSpread);

        float orbitalDistance = primary.radius + systemSize * this.systemParams.startOrbitRatioRandom.Evaluate(Random.value);
        float totalMass = 0;

        var direction = Random.value > 0.5f ? OrbitParameters.OrbitDirection.Clockwise : OrbitParameters.OrbitDirection.CounterClockwise;

        for (int i = 0; i < this.systemParams.maxPlanets && orbitalDistance < systemSize && totalMass < desiredTotalMass; i++)
        {
            float planetMass = massDistribution.CDF(this.systemParams.massDistributionFunctionSampleRange * orbitalDistance * this.systemParams.massVarianceRandom.Evaluate(Random.value) / systemSize) * desiredTotalMass - totalMass;

            float radius = this.systemParams.minPlanetRadius + planetMass * this.systemParams.planetMassRadiusRatioRandom.Evaluate(Random.value);
            orbitalDistance += radius;
            float moonSystemSize = allowMoons? planetMass * this.systemParams.moonSystemSizePlanetMassRatioRandom.Evaluate(Random.value) : 0;

            orbitalDistance += moonSystemSize;
            // Eccentricity of orbit controlled by gravitational force (higher = less eccentric) and own mass (higher = less eccentric), and distance from edge of system (higher = less eccentric
            float EccentricChance()
            {
                float force = primary.mass * planetMass / Mathf.Pow(orbitalDistance, 2);
                float distanceFromEdgeOfSystem = (systemSize - orbitalDistance) / systemSize;
                return Mathf.Clamp01(1 - Mathf.Pow(this.systemParams.eccentricityForceFactor * force + this.systemParams.eccentricityRimOrbitFactor * distanceFromEdgeOfSystem, 1f / 3f));
            }

            float orbitalDistance2 = orbitalDistance * (1 + this.systemParams.eccentricityAmountRandom.Evaluate(Random.value) * EccentricChance());

            float planetTemp = bodySpecs.PlanetTemp(orbitalDistance + primary.parameters.semiMajorAxis, primaryStar.radius, primaryStar.temp);
            var planet = new Body
            {
                randomKey = Random.Range(0, int.MaxValue),
                prefab = bodySpecs.RandomPlanet(planetMass, planetTemp).prefab,
                parameters = new OrbitParameters
                {
                    apoapsis = orbitalDistance2,
                    periapsis = orbitalDistance,
                    angle = Random.Range(0f, 360f),
                    offset = Random.Range(0f, 360f),
                    direction = direction,
                },
                radius = radius,
                mass = planetMass,
                temp = planetTemp
            };

            if (allowMoons)
            {
                this.GenerateBodies(
                    bodySpecs,
                    primaryStar,
                    planet,
                    systemSize: moonSystemSize,
                    desiredTotalMass: planet.mass * this.systemParams.moonSystemMassPlanetMassRatioRandom.Evaluate(Random.value),
                    massDistributionMedian: this.systemParams.massDistributionMedianRandom.Evaluate(Random.value),
                    massDistributionSpread: this.systemParams.massDistributionSpreadRandom.Evaluate(Random.value),
                    allowMoons: false);
            }

            orbitalDistance = (orbitalDistance2 + radius + moonSystemSize) * Mathf.Max(1, Mathf.Pow(this.systemParams.nextOrbitRadiusBase, this.systemParams.nextOrbitRadiusPowerRandom.Evaluate(Random.value)));
            primary.children.Add(planet);
            totalMass += planetMass;
        }
    }

    public Map Generate(BodySpecs bodySpecs)
    {
        Random.InitState((int)(DateTime.Now.Ticks % int.MaxValue));
        var map = new Map();

        float minDistance = this.minDistanceFactor / Mathf.Sqrt(this.numberOfSystems);

        float heightOffset = (1 - this.heightRatio) * 0.5f;

        // Generate the systems
        for (int i = 0; i < this.numberOfSystems; i++)
        {
            var position = new Vector2(Random.value, heightOffset + Random.value * this.heightRatio);

            for (int j = 0; j < 100 && map.systems.Any(s => Vector2.Distance(s.position, position) < minDistance); j++)
            {
                position = new Vector2(Random.value, heightOffset + Random.value * this.heightRatio);
            }

            char RandomLetter() => (char)((int)'A' + Random.Range(0, 'Z' - 'A'));
            string name = $"{RandomLetter()}{RandomLetter()}-{Mathf.FloorToInt(position.x * 100)},{Mathf.FloorToInt(position.y * 100)}";

            map.systems.Add(this.GenerateSystem(Random.Range(0, int.MaxValue), bodySpecs, name, position));
        }

        // Add the links
        var connectionTriangles = DelaunayCalculator.CalculateTriangulation(map.systems.Select(s => s.position).ToList());

        var acuteLinks = new List<(Link link, float score)>();
        for (int i = 0; i < connectionTriangles.Triangles.Count / 3; i++)
        {
            int SystemIndex(int pIndex) => connectionTriangles.Triangles[i * 3 + pIndex];
            Link MakeLink(int p0, int p1) => new Link { from = map.systems[p0], to = map.systems[p1] };

            // If the triangle has two small angles then remove the side that connects them,
            // if it has one small angle then 
            var links = new[] {
                MakeLink(SystemIndex(0), SystemIndex(1)),
                MakeLink(SystemIndex(1), SystemIndex(2)),
                MakeLink(SystemIndex(2), SystemIndex(0))
            };

            float area = Vector3.Cross(links[0].from.position - links[0].to.position, links[1].to.position - links[0].to.position).magnitude * 0.5f;

            IEnumerable<(Link link, float score)> LinkRemoveScores()
            {
                for (int j = 0; j < 3; j++)
                {
                    float len = (links[j].from.position - links[j].to.position).magnitude;
                    yield return (link: links[j], score: 2f * area / (len * len));
                }
            }

            acuteLinks.AddRange(LinkRemoveScores());

            map.links.AddRange(links.Except(map.links));
        }
        Assert.IsFalse(map.links.Any(l => map.links.Any(l2 => l.from == l2.to && l.to == l2.from)));
        // Trim acute links
        void TrimLinks(IEnumerable<Link> candidates)
        {
            foreach (var link in candidates)
            {
                // Test removing the link
                map.RemoveLink(link);
                var mapAStar = new MapAStar(map);
                if (mapAStar.AStarSearch(link.from, link.to) == null)
                {
                    map.links.Add(link);
                }
            }
        }

        TrimLinks(acuteLinks.Where(l => l.score < this.linkAcuteRemoveScore).OrderBy(l => l.score).Select(l => l.link));

        // Trim obtuse links
        var linkSum = new Dictionary<Link, float>();
        void AddLinkAngle(Link link, float angle)
        {
            linkSum[link] = linkSum.ContainsKey(link) ? Mathf.Max(linkSum[link], angle) : angle;
        }

        foreach (var s in map.systems)
        {
            var links = map.GetJumpTargets(s).OrderBy(l => Vector2.SignedAngle(Vector2.right, l.system.position - s.position)).ToList();
            float Angle(Vector2 a, Vector2 b, Vector2 c) => Vector2.Angle(a - b, c - b);
            for (int j = 0; j < links.Count; j++)
            {
                float angle = Angle(links[j].system.position,
                    s.position,
                    links[(j + 1) % links.Count].system.position);
                AddLinkAngle(links[j].link, angle);
                AddLinkAngle(links[(j + 1) % links.Count].link, angle);
            }
        }

        var scoreLinks = linkSum.Select(kv => (link: kv.Key, score: 360f - kv.Value)).ToList();
        TrimLinks(scoreLinks.Where(ls => ls.score < 360f * this.linkObtuseRemoveScore).OrderBy(l => l.score).Select(l => l.link));

        // Reduce number of links by the reduction ratio
        int targetCount = (int)(map.links.Count * this.linkReductionRatio);

        while (map.links.Count > targetCount)
        {
            var randomLinks = map.links.OrderBy(a => Random.value).Take(map.links.Count - targetCount).ToList();
            int prevCount = map.links.Count;
            TrimLinks(randomLinks);
            // If we didn't manage to remove any more then quit
            if (prevCount == map.links.Count)
                break;
        }

        return map;
    }
}
