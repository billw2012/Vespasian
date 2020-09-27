/// <summary>
/// The heart of the implementation.
/// Provides the core functionality of the A* algorithm.
/// </summary>

using System.Collections.Generic;

public abstract class AStar<T>
{
    protected class Cell
    {
        public Cell(T position)
        {
            this.position = position;
        }

        public T position;
        public Cell parent;
        public float cost, heuristic;
        public float f => this.cost + this.heuristic;
    }

    protected abstract bool EqualPosition(T a, T b);
    protected abstract Cell GetNearestCell(T position);
    protected abstract IEnumerable<Cell> GetNeighbours(Cell cell);
    protected abstract float CalcHeuristic(T from, T to);

    // We make these members as opposed to local variables so we can keep reallocation to a minimum
    readonly List<Cell> openList = new List<Cell>();
    readonly List<Cell> closedList = new List<Cell>();

    public IEnumerable<T> AStarSearch(T start, T goal)
    {
        this.openList.Clear();
        this.closedList.Clear();

        var startCell = this.GetNearestCell(start);
        var goalCell = this.GetNearestCell(goal);

        startCell.heuristic = this.CalcHeuristic(startCell.position, goal);
        this.openList.Add(startCell);

        while (this.openList.Count > 0)
        {
            var bestCell = GetBestCell(this.openList);
            this.openList.Remove(bestCell);

            var neighbours = this.GetNeighbours(bestCell);
            foreach(var curCell in neighbours)
            {
                if (curCell == goalCell)
                {
                    curCell.parent = bestCell;
                    return ConstructPath(curCell);
                }

                float g = bestCell.cost + this.CalcHeuristic(curCell.position, bestCell.position);
                float h = this.CalcHeuristic(curCell.position, goal);

                if (this.openList.Contains(curCell) && curCell.f < (g + h))
                    continue;
                if (this.closedList.Contains(curCell) && curCell.f < (g + h))
                    continue;

                curCell.cost = g;
                curCell.heuristic = h;
                curCell.parent = bestCell;

                if (!this.openList.Contains(curCell))
                    this.openList.Add(curCell);
            }

            if (!this.closedList.Contains(bestCell))
                this.closedList.Add(bestCell);
        }

        return null;
    }


    // We could shorten this with a nice linq statement. However, linq has a considerable overhead compared to classic iteration.
    private static Cell GetBestCell(IList<Cell> openList)
    {
        Cell result = null;
        float currentF = float.PositiveInfinity;

        for (int i = 0; i < openList.Count; i++)
        {
            var cell = openList[i];

            if (cell.f < currentF)
            {
                currentF = cell.f;
                result = cell;
            }
        }

        return result;
    }


    private static IEnumerable<T> ConstructPath(Cell destination)
    {
        var path = new List<T>() { destination.position };

        var current = destination;
        while (current.parent != null)
        {
            current = current.parent;
            path.Add(current.position);
        }

        path.Reverse();
        return path;
    }
}