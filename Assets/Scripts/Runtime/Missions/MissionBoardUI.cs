using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionBoardUI : MissionListUIBase
{
    protected override IEnumerable<IMissionBase> MissionList => this.missions.availableMissions;
    protected override GameObject CreateUI(IMissionFactory factory, IMissionBase mission, Transform parent) => factory.CreateBoardUI(this.missions, mission, parent);
}
