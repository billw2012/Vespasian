using System.Collections.Generic;
using UnityEngine;

public class MissionActiveUI : MissionListUIBase
{
    protected override IEnumerable<IMissionBase> GetMissions() => this.missions.activeMissions;

    protected override GameObject CreateUI(IMissionFactory factory, IMissionBase mission, Transform parent) => factory.CreateActiveUI(this.missions, mission, parent);
}
