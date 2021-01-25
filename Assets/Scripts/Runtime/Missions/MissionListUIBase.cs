using System.Collections.Generic;
using UnityEngine;

public abstract class MissionListUIBase : MonoBehaviour
{
    public GameObject grid;

    public Missions missions { get; private set; }

    private void OnEnable()
    {
        this.missions = FindObjectOfType<Missions>();
        this.missions.OnMissionsChanged += this.UpdateList;
        this.InitUI();
    }

    protected virtual void InitUI() => this.UpdateUI();

    protected virtual void UpdateUI() => this.UpdateList();

    private void UpdateList()
    {
        foreach (Transform itemUI in this.grid.transform)
        {
            Destroy(itemUI.gameObject);
        }

        foreach (var mission in this.MissionList)
        {
            this.CreateUI(this.missions.GetFactory(mission.Factory), mission, this.grid.transform);
        }
    }

    protected abstract IEnumerable<IMissionBase> MissionList { get; }
    protected abstract GameObject CreateUI(IMissionFactory factory, IMissionBase mission, Transform parent);
}