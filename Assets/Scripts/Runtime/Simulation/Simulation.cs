using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


public interface ISimUpdate
{
    void SimUpdate(Simulation simulation, int simTick, int timeStep);
    void SimRefresh(Simulation simulation);
}

/// <summary>
/// All our custom simulation is updated by this class.
/// It allows for time warping (by integer multipliers only).
/// It ensures order of update is strict.
/// </summary>
public class Simulation : MonoBehaviour
{
    public GameConstants constants;

    public int tickStep { get; set; } = 1;

    public int simTick { get; set; } = 0;

    public float time => this.simTick * Time.fixedDeltaTime;

    public float dt => this.tickStep * Time.fixedDeltaTime;

    private List<MonoBehaviour> simulatedObjects;

    private SimModel model;

    private void OnValidate()
    {
        Assert.IsNotNull(this.constants);
    }

    private void Start()
    {
        Assert.IsNotNull(this.constants);
        this.Refresh();
    }

    private void FixedUpdate()
    {
        this.model.DelayedInit();

        this.simTick += this.tickStep;

        // Update game objects from model (we use the simModels orbit list so we keep consistent ordering)
        foreach (var o in this.model.orbits
            .Where(o => o != null))
        {
            o.SimUpdate(this, this.simTick);
        }

        foreach (var s in this.simulatedObjects
            .Where(s => s != null)
            .Where(s => s.gameObject.activeInHierarchy && s.isActiveAndEnabled)
            .OfType<ISimUpdate>())
        {
            s.SimUpdate(this, this.simTick, this.tickStep);
        }
    }

    public void Refresh()
    {
        this.model = new SimModel();
        this.model.DelayedInit();
        this.simulatedObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISimUpdate>().OfType<MonoBehaviour>().ToList();
        foreach (var s in this.simulatedObjects.OfType<ISimUpdate>())
        {
            s.SimRefresh(this);
        }
    }

    public void Register(ISimUpdate simUpdate)
    {
        if(this.simulatedObjects != null && !this.simulatedObjects.Contains((MonoBehaviour)simUpdate))
        {
            this.simulatedObjects.Add((MonoBehaviour)simUpdate);
        }    
    }  
    
    public void Unregister(ISimUpdate simUpdate) => this.simulatedObjects.Remove((MonoBehaviour)simUpdate);

    public SectionedSimPath CreateSectionedSimPath(Vector3 startPosition, Vector3 startVelocity, int targetTicks, float collisionRadius, bool disableFuturePath = false, int sectionTicks = 200)
    {
        return new SectionedSimPath(this.model, this.simTick, startPosition, startVelocity, targetTicks, Time.fixedDeltaTime, this.constants.GravitationalConstant, this.constants.GravitationalRescaling, collisionRadius, disableFuturePath, sectionTicks);
    }
}