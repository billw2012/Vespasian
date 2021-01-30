﻿using System.Collections.Generic;
using UnityEngine;

public class MissionMapUI : MissionListUIBase
{
    private List<IMissionBase> missionList = new List<IMissionBase>();

    public void UpdateMissionList(BodyRef selectedSystem)
    {
        this.missionList = this.missions != null 
                ? this.missions.GetActiveMissionsInSystem(selectedSystem)
                : new List<IMissionBase>();
        this.UpdateUI();
    }

    protected override IEnumerable<IMissionBase> MissionList => this.missionList;

    protected override GameObject CreateUI(IMissionFactory factory, IMissionBase mission, Transform parent) => factory.CreateActiveUI(this.missions, mission, parent);
}