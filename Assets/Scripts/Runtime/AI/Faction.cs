using IngameDebugConsole;
using System;
using System.Linq;
using UnityEngine;

public class Faction : MonoBehaviour
{
    [Flags]
    public enum FactionType
    {
        None   = 0,
        Player = 1 << 0,
        Pirate = 1 << 1,
        Alien  = 1 << 2,
    }
    
    public FactionType factionType = FactionType.None;
    
    public DataCatalog data;
    private FactionExpansion factionExpansion;

    private void Awake()
    {
        this.factionExpansion = this.GetComponent<FactionExpansion>();
    }

    public void PopulateMap(Map map) => this.factionExpansion?.PopulateMap(map);

    public void SpawnSystem(SolarSystem system, GameObject rootBody)
    {
        this.GetComponent<FactionSpawns>()?.SpawnSystem(system, rootBody);
    }

    [ConsoleMethod("faction.reveal-map", "Reveal all planets to the faction AI")]
    public static void DebugFactionRevealMap(string factionMask, string dataMask)
    {
        var map = FindObjectOfType<MapComponent>()?.map;
        if (map != null)
        {
            var factionType = (FactionType)Enum.Parse(typeof(FactionType), factionMask, ignoreCase: true);
            var factions = FindObjectsOfType<Faction>()
                .Where(f => factionType.HasFlag(f.factionType))
                .ToList();

            foreach (var body in map.systems.SelectMany(s => s.AllBodies().OfType<StarOrPlanet>()))
            {
                foreach (var faction in factions)
                {
                    faction.data.AddData(body.bodyRef, (DataMask)Enum.Parse(typeof(DataMask), dataMask, ignoreCase: true));
                }
            }
        }
    }
}
