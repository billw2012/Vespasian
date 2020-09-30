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

    public static SolarSystem GenerateSystem(int randomKey, BodySpecs bodySpecs, string name, Vector2 position)
    {
        Random.InitState(randomKey);

        var sys = new SolarSystem { name = name, position = position };

        float starRadius = MathX.RandomGaussian(5, 30); // What is the actual correct weighting function for star mass? Probably log normal?
        float starTemp = MathX.RandomGaussian(0.4f, 5); // 4000K to 50000K
        float starMass = starRadius * MathX.RandomGaussian(0.5f, 1.5f);

        var mainSpec = bodySpecs.RandomStar(starTemp, starMass);
        sys.main = new Body
        {
            randomKey = Random.Range(0, int.MaxValue),
            prefab = mainSpec.prefab,
            radius = starRadius,
            temp = starTemp,
            mass = starMass,
        };
        GenerateBodies(
            bodySpecs,
            sys.main,
            sys.main,
            systemSize: sys.main.mass * MathX.RandomGaussian(1f, 15f),
            desiredTotalMass: sys.main.mass * MathX.RandomGaussian(0f, 1f),
            massDistributionMedian: Random.Range(0.1f, 2.5f),
            massDistributionSpread: Random.Range(0.1f, 0.9f));

        //int planets = Random.Range(0, 10);

        ////float radius = 
        //for (int i = 0; i < planets; i++)
        //{
        //    massDistribution.
        //}

        //// Place holder system generation.
        //// TODO: make this way better lol
        //// Probably we want a set of different generators for certain types of systems?
        //int planets = Random.Range(0, 7);
        //float minRadius = mainSpec.distance + mainSpec.distanceRange + 10;
        //float orbitDistance = Random.Range(minRadius, minRadius * 2);
        //for (int i = 0; i < planets; i++)
        //{
        //    var planetSpec = bodySpecs.RandomPlanet();
        //    var planet = new Body
        //    {
        //        prefab = planetSpec.prefab,
        //        parameters = new OrbitParameters
        //        {
        //            periapsis = orbitDistance,
        //            apoapsis = orbitDistance
        //        }
        //    };

        //    sys.main.children.Add(planet);

        //    orbitDistance *= Random.Range(1.25f, 2.25f);
        //}

        return sys;
    }

    // Bodies are created by assuming a Log Normal distribution of mass wrt distance from their primary.
    // We use the CDF to slice the mass function into sections for each planet
    // See https://www.desmos.com/calculator/xfskhgkr9l
    private static void GenerateBodies(BodySpecs bodySpecs, Body primaryStar, Body primary, float systemSize, float desiredTotalMass, float massDistributionMedian, float massDistributionSpread, bool allowMoons = true)
    {
        // We will sample this in a range of 0 - 10
        var massDistribution = new MathX.LogNormal(massDistributionMedian, massDistributionSpread);

        float orbitalDistance = primary.radius + systemSize * MathX.RandomGaussian(0, 0.1f);
        float totalMass = 0;

        while (orbitalDistance < systemSize && totalMass < desiredTotalMass)
        {
            float planetMass = massDistribution.CDF(10f * orbitalDistance * MathX.RandomGaussian(0.75f, 1.25f) / systemSize) * desiredTotalMass - totalMass;
            float planetTemp = BodySpecs.PlanetTemp(orbitalDistance, primaryStar.radius, primaryStar.temp);
            var planetSpec = bodySpecs.RandomPlanet(planetTemp, planetMass);


            float radius = 3 + planetMass * MathX.RandomGaussian(0.5f, 1.5f);
            orbitalDistance += radius;
            float moonSystemSize = planetMass * MathX.RandomGaussian(0f, 2.5f);
            orbitalDistance += moonSystemSize;

            float orbitalDistance2 = orbitalDistance * MathX.RandomGaussian(1, 1 + Mathf.Pow(1 - (systemSize - orbitalDistance) / systemSize, 4));

            var planet = new Body
            {
                randomKey = Random.Range(0, int.MaxValue),
                prefab = planetSpec.prefab,
                parameters = new OrbitParameters
                {
                    apoapsis = orbitalDistance2,
                    periapsis = orbitalDistance,
                    angle = Random.Range(0f, 360f),
                    offset = Random.Range(0f, 360f),
                    direction = Random.value > 0.5f ? OrbitParameters.OrbitDirection.Clockwise : OrbitParameters.OrbitDirection.CounterClockwise,
                },
                radius = radius,
                mass = planetMass,
                temp = planetTemp
            };

            GenerateBodies(
                bodySpecs,
                primaryStar,
                planet,
                systemSize: moonSystemSize,
                desiredTotalMass: planet.mass * MathX.RandomGaussian(0f, 0.5f),
                massDistributionMedian: Random.Range(0.1f, 2.5f),
                massDistributionSpread: Random.Range(0.1f, 0.9f),
                allowMoons: false);

            orbitalDistance = (orbitalDistance2 + radius + moonSystemSize) * Mathf.Pow(2, MathX.RandomGaussian(0.5f, 1.25f));
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

            map.systems.Add(GenerateSystem(Random.Range(0, int.MaxValue), bodySpecs, name, position));
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
