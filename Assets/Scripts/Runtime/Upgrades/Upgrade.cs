using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/*
 * GameLogic: New, Save, Load, UpgradeDef list, Map
 * 
 * 
 */
public class UpgradeDef : ScriptableObject
{
    public int cost = 0;
    public float mass = 0;

    public GameObject shipPartPrefab;
    public GameObject shopUIPrefab;

    public List<UpgradeDef> replaces;
    public List<UpgradeDef> requires;

    public static GameObject FindUpgrade(Transform ship, string name)
    {
        var transform = ship.transform.Find(name);
        return transform != null ? transform.gameObject : null;
    }

    public static bool HasUpgrade(Transform ship, string name)
    {
        return FindUpgrade(ship, name) != null;
    }

    public bool Allowed(Transform ship)
    {
        return this.requires.All(u => HasUpgrade(ship, u.name));
    }

    public void Install(Transform ship)
    {
        var obj = Object.Instantiate(this.shipPartPrefab, ship);
        obj.name = this.name;
        var upgradeLogic = obj.GetComponent<UpgradeLogic>();
        if (upgradeLogic != null)
        {
            upgradeLogic.Install();
        }
    }

    public void Uninstall(Transform ship)
    {
        var obj = FindUpgrade(ship, this.name);
        if (obj != null)
        {
            var upgradeLogic = obj.GetComponent<UpgradeLogic>();
            if (upgradeLogic != null)
            {
                upgradeLogic.Uninstall();
            }
            obj.transform.SetParent(null);
            Destroy(obj);

            Debug.Log($"Uninstalled upgrade {this.name} from ship {ship.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Could not uninstall upgrade {this.name} as it didn't exist on ship {ship.gameObject.name}");
        }
    }

    public void AddUI(RectTransform parent)
    {
        var obj = Object.Instantiate(this.shopUIPrefab, parent);
        obj.name = this.name;
    }
}

public abstract class UpgradeLogic : MonoBehaviour
{
    public abstract void Install();
    public abstract void Uninstall();
}