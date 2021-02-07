using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    // Todo or should we use proxies instead?
    private List<WeaponComponentBase> weapons = new List<WeaponComponentBase>();

    private int currentWeaponId = 0;


    // These are called from WeaponComponentBase.Install/Uninstall
    public void RegisterWeapon(WeaponComponentBase weapon)
    {
        if (!weapons.Contains(weapon))
            this.weapons.Add(weapon);
    }

    public void UnregisterWeapon(WeaponComponentBase weapon)
    {
        this.weapons.Remove(weapon);

        // If current weapon is removed now, revert to first weapon in the list
        if (this.currentWeaponId >= this.weapons.Count)
            this.currentWeaponId = 0;
    }

    // ---------------------------------------




    // Weapon cycling
    public void CycleCurrentWeapon(bool forward)
    {
        int c = this.weapons.Count;

        if (forward)
            this.currentWeaponId = (this.currentWeaponId + 1) % c;
        else
        {
            this.currentWeaponId--;
            if (this.currentWeaponId < 0)
                this.currentWeaponId = c - 1;
        }
    }


    public WeaponComponentBase GetWeapon(int weaponId)
    {
        if (weaponId >= this.weapons.Count)
            return null;

        return this.weapons[weaponId];
    }

    public WeaponComponentBase GetCurrentWeapon()
    {
        if (this.currentWeaponId >= this.weapons.Count)
            return null;

        return this.weapons[this.currentWeaponId];
    }

    public List<WeaponComponentBase> GetAllWeapons()
    {
        return new List<WeaponComponentBase>(this.weapons);
    }

    public int weaponCount { get => this.weapons.Count; }
}
