using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapAStar : AStar<SolarSystem>
{
    readonly Dictionary<SolarSystem, Cell> cells;
    readonly Dictionary<Cell, IEnumerable<Cell>> links;

    public MapAStar(Map map)
    {
        this.cells = map.systems.ToDictionary(s => s, s => new Cell(s));

        this.links = new Dictionary<Cell, IEnumerable<Cell>>();
        foreach(var cell in this.cells.Values)
        {
            this.links[cell] = map.GetConnected(cell.position).Select(s => this.cells[s.system]);
        }
    }

    protected override float CalcHeuristic(SolarSystem from, SolarSystem to) => Vector2.Distance(from.position, to.position);
    protected override bool EqualPosition(SolarSystem a, SolarSystem b) => a == b;
    protected override Cell GetNearestCell(SolarSystem position) => this.cells[position];
    protected override IEnumerable<Cell> GetNeighbours(Cell cell) => this.links[cell];
}
