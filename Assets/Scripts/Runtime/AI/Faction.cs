using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

public class Faction : MonoBehaviour, ISavable
{
    public DataCatalog data;
    public string stationSpecId;

    [NonSerialized]
    [Saved]
    [RegisterSavableType]
    public HashSet<BodyRef> stations = new HashSet<BodyRef>();
 
    private void Awake()
    {
        FindObjectOfType<SaveSystem>().RegisterForSaving(this);
        FindObjectOfType<MapComponent>().MapGenerated += this.OnMapGenerated;
    }

    private void OnMapGenerated()
    {
        // Reveal all station planets to the player
        var playerData = Object.FindObjectOfType<PlayerController>().GetComponent<DataCatalog>();
        foreach (var station in this.stations)
        {
            playerData.AddData(station, DataMask.All);
        }
    }

    /// <summary>
    /// Called from a background thread to populate the system map with faction stuff, during
    /// map generation.
    /// </summary>
    /// <param name="map"></param>
    public void PopulateMap(Map map)
    {
        var startingPlanet = map.systems
            .OrderBy(s => s.position.x)
            .Select(s => s.AllBodies()
                .OfType<StarOrPlanet>()
                .Where(p => p.habitability > 0.25f)
                .SelectRandom())
            .Where(b => b != null)
            .Take(map.systems.Count / 10 + 1)
            .SelectRandom()
            ;

        if (startingPlanet == null)
        {
            Debug.LogError($"Couldn't find suitable home world for faction {this.gameObject}");
            return;
        }

        startingPlanet.children.Add(new Station(startingPlanet.bodyRef.systemId)
        {
            specId = this.stationSpecId,
            parameters = new OrbitParameters
            {
                periapsis = startingPlanet.radius + 5,
                apoapsis = startingPlanet.radius + 5,
            }
        });

        // Update our station list on the main thread, wait for it so further processing can see it
        ThreadingX.RunOnUnityThreadAsync(() =>
        {
            this.stations.Add(startingPlanet.bodyRef);
        }).Wait();
    }
}
