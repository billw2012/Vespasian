using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissionMapUI : MissionListUIBase
{
    private List<IMissionBase> _missionList = new List<IMissionBase>();

    public void UpdateMissionList(Missions missions, BodyRef selectedSystem)
    {
        this._missionList = missions.GetActiveMissionsInSystem(selectedSystem);
        this.UpdateUI();
    }

    protected override IEnumerable<IMissionBase> MissionList
    {
        get
        {
            return this._missionList;
        }
    }

    protected override GameObject CreateUI(IMissionFactory factory, IMissionBase mission, Transform parent) => factory.CreateActiveUI(this.missions, mission, parent);
}