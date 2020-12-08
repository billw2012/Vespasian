using GK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

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
        [Tooltip("Final orbits are all scaled by this amount"), Range(1, 10)]
        public float finalOrbitScaling = 1f;
        [Tooltip("Final masses are all scaled by this amount"), Range(0, 100)]
        public float finalMassScaling = 1f;


        public float massDistributionFunctionSampleRange = 10f;
        [Tooltip("Mass distribution function median")]
        public WeightedRandom massDistributionMedianRandom = new WeightedRandom { min = 0.1f, max = 2.5f };
        [Tooltip("Mass distribution function spread")]
        public WeightedRandom massDistributionSpreadRandom = new WeightedRandom { min = 0.1f, max = 0.9f };

        [Tooltip("Ratio of system size to star mass")]
        public WeightedRandom systemSizeStarMassRatioRandom = new WeightedRandom { min = 1f, max = 15f, gaussian = true };
        [Tooltip("Ratio of system mass to star mass")]
        public WeightedRandom systemMassStarMassRatioRandom = new WeightedRandom { min = 0f, max = 1f, gaussian = true };

        public int maxPlanets = 10;

        [Tooltip("Start orbit min from primary")]
        public float startOrbitMinFromPrimary = 5f;

        [Tooltip("Min orbit separation")]
        public float minOrbitSeperation = 5f;

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
        //[Tooltip("Ratio of planet mass to planet radius")]
        //public WeightedRandom planetMassRadiusRatioRandom = new WeightedRandom { min = 0.5f, max = 1.5f, gaussian = true };
        [Tooltip("Ratio of moon system size to planet mass")]
        public WeightedRandom moonSystemSizePlanetMassRatioRandom = new WeightedRandom { min = 0.5f, max = 1.5f, gaussian = true };
        [Tooltip("Ratio of moon system mass to planet mass")]
        public WeightedRandom moonSystemMassPlanetMassRatioRandom = new WeightedRandom { min = 0f, max = 1f, gaussian = true };

        [Range(0, 1)]
        public float beltChance = 0.25f;

        public WeightedRandom beltRelativeSystemDistance = new WeightedRandom { min = 0.1f, max = 0.4f, gaussian = true };

        public WeightedRandom cometCountRandom = new WeightedRandom { min = 0f, max = 5f };
    };

    public SystemGeneratorParameters systemParams;

    public SolarSystem GenerateSystem(int randomKey, BodySpecs bodySpecs, string name, Vector2 position)
    {
        var rng = new RandomX(randomKey);

        var sys = new SolarSystem { name = name, position = position };

        var mainSpec = bodySpecs.RandomStar(rng);
        float starMass = mainSpec.massRandom.Evaluate(rng);
        float starTemp = mainSpec.tempRandom.Evaluate(rng);
        float starDensity = mainSpec.densityRandom.Evaluate(rng);
        float starRadius = starMass / starDensity; // obviously not the correct formula...

        var direction = rng.value > 0.5f ? OrbitParameters.OrbitDirection.Clockwise : OrbitParameters.OrbitDirection.CounterClockwise;

        float systemSize = starMass * this.systemParams.systemSizeStarMassRatioRandom.Evaluate(rng);
        var planets = this.GenerateBodies(
            rng,
            bodySpecs, 
            starTemp, starRadius, 0,
            starRadius, starMass,
            systemSize: systemSize,
            direction: direction,
            desiredTotalMass: starMass * this.systemParams.systemMassStarMassRatioRandom.Evaluate(rng),
            massDistributionMedian: this.systemParams.massDistributionMedianRandom.Evaluate(rng),
            massDistributionSpread: this.systemParams.massDistributionSpreadRandom.Evaluate(rng));

        if(rng.value <= this.systemParams.beltChance)
        {
            float beltDist = this.systemParams.beltRelativeSystemDistance.Evaluate(rng) * systemSize;

            var belt = bodySpecs.RandomBelt(rng, beltDist);
            //var randomPlanet = planets.Shuffle().FirstOrDefault();

            //float orbit = 0;
            //if (randomPlanet != null)
            //{
            //    planets.Remove(randomPlanet);
            //    orbit = randomPlanet.parameters.periapsis;
            //}
            //else
            //{
            //    orbit = Random.Range(starRadius * 2, this.systemParams.maxBeltOrbit);
            //}

            sys.belts.Add(new Belt {
                specId = belt.id,
                radius = beltDist,
                width = rng.Range(3f, 10f),
                direction = direction,
            });
        }

        int cometCount = Mathf.FloorToInt(this.systemParams.cometCountRandom.Evaluate(rng));
        sys.comets.AddRange(this.GenerateComets(rng, bodySpecs, cometCount, starRadius, systemSize));

        sys.main = new StarOrPlanet
        {
            randomKey = rng.Range(0, int.MaxValue),
            specId = mainSpec.id,
            density = starDensity,
            radius = starRadius,
            temp = starTemp,
            mass = starMass * this.systemParams.finalMassScaling,
            children = planets
        };

        return sys;
    }

    private List<Comet> GenerateComets(RandomX rng, BodySpecs bodySpecs, int cometCount, float starRadius, float systemSize)
    {
        var comets = new List<Comet>();

        for (int i = 0; i < cometCount; i++)
        {
            var spec = bodySpecs.RandomComet(rng);
            float periapsis = Mathf.Max(starRadius * 2, spec.minApproach, spec.relativePeriapsisRandom.Evaluate(rng) * systemSize);

            float ecc = spec.eccentricityRandom.Evaluate(rng);
            float apoapsis = (1f + ecc) * periapsis / (1f - ecc);

            // float apoapsis = Mathf.Max(periapsis * 3, this.systemParams.cometRelativeApoapsisRandom.Evaluate(Random.value) * systemSize);

            var direction = rng.value > 0.5f ? OrbitParameters.OrbitDirection.Clockwise : OrbitParameters.OrbitDirection.CounterClockwise;
            var comet = new Comet
            {
                 randomKey = rng.Range(0, int.MaxValue),
                 specId = spec.id,
                 parameters = new OrbitParameters
                 {
                     periapsis = periapsis * this.systemParams.finalOrbitScaling,
                     apoapsis = apoapsis * this.systemParams.finalOrbitScaling,
                     angle = rng.Range(0f, 360f),
                     offset = rng.Range(0f, 360f),
                     direction = direction,
                 }
            };
            comets.Add(comet);
        }

        return comets;
    }

    // Bodies are created by assuming a Log Normal distribution of mass wrt distance from their primary.
    // We use the CDF to slice the mass function into sections for each planet
    // See https://www.desmos.com/calculator/xfskhgkr9l
    private List<StarOrPlanet> GenerateBodies(
        RandomX rng,
        BodySpecs bodySpecs, 
        float starTemp,
        float starRadius,
        float starDistance,
        float primaryRadius,
        float primaryMass,
        float systemSize,
        OrbitParameters.OrbitDirection direction,
        float desiredTotalMass,
        float massDistributionMedian,
        float massDistributionSpread,
        bool allowMoons = true)
    {
        // We will sample this in a range of 0 - 10
        var massDistribution = new MathX.LogNormal(massDistributionMedian, massDistributionSpread);

        float orbitalDistance = primaryRadius + this.systemParams.startOrbitMinFromPrimary + systemSize * this.systemParams.startOrbitRatioRandom.Evaluate(rng);
        float totalMass = 0;

        var bodies = new List<StarOrPlanet>();

        for (int i = 0; i < this.systemParams.maxPlanets && orbitalDistance < systemSize && totalMass < desiredTotalMass; i++)
        {
            float planetMass = massDistribution.CDF(this.systemParams.massDistributionFunctionSampleRange * orbitalDistance * this.systemParams.massVarianceRandom.Evaluate(rng) / systemSize) * (desiredTotalMass - totalMass);

            float planetTemp = bodySpecs.PlanetTemp(starDistance + orbitalDistance, starRadius, starTemp);
            var planetSpec = bodySpecs.RandomPlanet(rng, planetMass, planetTemp);

            float planetDensity = planetSpec.densityRandom.Evaluate(rng);
            float planetRadius = this.systemParams.minPlanetRadius + planetMass / planetDensity;
                //+ planetMass * this.systemParams.planetMassRadiusRatioRandom.Evaluate(Random.value);

            float moonSystemSize = allowMoons? planetMass * this.systemParams.moonSystemSizePlanetMassRatioRandom.Evaluate(rng) : 0;

            var moons = allowMoons? this.GenerateBodies(
                rng,
                bodySpecs, 
                starTemp, starRadius, starDistance + orbitalDistance,
                planetRadius, planetMass,
                systemSize: moonSystemSize,
                direction: rng.value > 0.5f ? OrbitParameters.OrbitDirection.Clockwise : OrbitParameters.OrbitDirection.CounterClockwise,
                desiredTotalMass: planetMass * this.systemParams.moonSystemMassPlanetMassRatioRandom.Evaluate(rng),
                massDistributionMedian: this.systemParams.massDistributionMedianRandom.Evaluate(rng),
                massDistributionSpread: this.systemParams.massDistributionSpreadRandom.Evaluate(rng),
                allowMoons: false) : new List<StarOrPlanet>();

            // Adjust orbital distance by moon system actual size and planet radius, so we don't waste space
            moonSystemSize = moons.Any()? moons.Max(c => c.parameters.apoapsis) : 0;
            orbitalDistance = orbitalDistance + planetRadius + moonSystemSize + this.systemParams.minOrbitSeperation;


            // Eccentricity of orbit controlled by gravitational force (higher = less eccentric) and own mass (higher = less eccentric), and distance from edge of system (higher = less eccentric
            float EccentricChance()
            {
                float force = primaryMass * planetMass / Mathf.Pow(orbitalDistance, 2);
                float distanceFromEdgeOfSystem = Mathf.Max(0, (systemSize - orbitalDistance) / systemSize);
                return Mathf.Clamp01(1 - Mathf.Pow(this.systemParams.eccentricityForceFactor * force + this.systemParams.eccentricityRimOrbitFactor * distanceFromEdgeOfSystem, 1f / 3f));
            }

            float orbitalDistance2 = orbitalDistance * (1 + this.systemParams.eccentricityAmountRandom.Evaluate(rng) * EccentricChance());


            var planet = new StarOrPlanet
            {
                randomKey = rng.Range(0, int.MaxValue),
                specId = bodySpecs.RandomPlanet(rng, planetMass, planetTemp).id,
                parameters = new OrbitParameters
                {
                    periapsis = orbitalDistance * this.systemParams.finalOrbitScaling,
                    apoapsis = orbitalDistance2 * this.systemParams.finalOrbitScaling,
                    angle = rng.Range(0f, 360f),
                    offset = rng.Range(0f, 360f),
                    direction = direction,
                },
                children = moons,
                density = planetDensity,
                radius = planetRadius,
                mass = planetMass * this.systemParams.finalMassScaling,
                temp = planetTemp,
                resources = planetSpec.resourcesRandom.Evaluate(rng),
                habitability = planetSpec.habitabilityRandom.Evaluate(rng),
            };
            bodies.Add(planet);

            // Move to next orbital distance
            orbitalDistance = Mathf.Max(orbitalDistance2 + planetRadius + moonSystemSize + this.systemParams.minOrbitSeperation, orbitalDistance2 * Mathf.Max(1, Mathf.Pow(this.systemParams.nextOrbitRadiusBase, this.systemParams.nextOrbitRadiusPowerRandom.Evaluate(rng))));
            totalMass += planetMass;
        }
        return bodies;
    }

    public async Task<Map> GenerateAsync(BodySpecs bodySpecs)
    {
        return await Task.Run(() =>
        {
            var rng = new RandomX((int)(DateTime.Now.Ticks % int.MaxValue));
            // Random.InitState((int)(DateTime.Now.Ticks % int.MaxValue));
            var map = new Map();

            float minDistance = this.minDistanceFactor / Mathf.Sqrt(this.numberOfSystems);

            float heightOffset = (1 - this.heightRatio) * 0.5f;

            // Generate the systems
            for (int i = 0; i < this.numberOfSystems; i++)
            {
                var position = new Vector2(rng.value, heightOffset + rng.value * this.heightRatio);

                for (int j = 0; j < 100 && map.systems.Any(s => Vector2.Distance(s.position, position) < minDistance); j++)
                {
                    position = new Vector2(rng.value, heightOffset + rng.value * this.heightRatio);
                }

                char RandomLetter() => (char)((int)'A' + rng.Range(0, 'Z' - 'A'));
                string name = $"{RandomLetter()}{RandomLetter()}-{Mathf.FloorToInt(position.x * 100)},{Mathf.FloorToInt(position.y * 100)}";

                map.AddSystem(this.GenerateSystem(rng.Range(0, int.MaxValue), bodySpecs, name, position));
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
                var links = map.GetConnected(s).OrderBy(l => Vector2.SignedAngle(Vector2.right, l.system.position - s.position)).ToList();
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
                var randomLinks = map.links.OrderBy(a => rng.value).Take(map.links.Count - targetCount).ToList();
                int prevCount = map.links.Count;
                TrimLinks(randomLinks);
                // If we didn't manage to remove any more then quit
                if (prevCount == map.links.Count)
                    break;
            }

            return map;
        });
    }
}
