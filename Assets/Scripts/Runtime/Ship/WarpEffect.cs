using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WarpEffect : MonoBehaviour
{
    [Range(1, 50)]
    public float effectMultiplier = 5;

    public float effectBaseSize = 10;


    [NonSerialized]
    public float amount;
    [NonSerialized]
    public Vector3 direction;
    [NonSerialized]
    public float turningAmount;


    private class Pfx
    {
        public ParticleSystem pfx;
        public ParticleSystemRenderer renderer;

        public Color startColor;
        public Color startColorMin;
        public Color startColorMax;

        public Pfx(ParticleSystem pfx)
        {
            this.pfx = pfx;
            this.renderer = pfx.gameObject.GetComponent<ParticleSystemRenderer>();
            this.startColor = pfx.main.startColor.color;
            this.startColorMin = pfx.main.startColor.colorMin;
            this.startColorMax = pfx.main.startColor.colorMax;
        }

        public void Update(float speed, float speedRatio, float turningAmount)
        {
            //this.pfx.SetMainValues(main =>
            //{
            //    main.startLifetime = this.pfx.shape.radius / Mathf.Max(1f, speed);
            //});
            this.pfx.SetVelocityOverLifetimeValues(vol => { vol.y = speed; vol.orbitalZ = speed * turningAmount; });
            this.pfx.SetColorOverLifetimeValues(col => col.color = new ParticleSystem.MinMaxGradient(Color.white.SetA(speedRatio)));
        }
    }

    private List<Pfx> effects;


    private void Start()
    {
        this.effects = this.GetComponentsInChildren<ParticleSystem>().Select(pfx => new Pfx(pfx)).ToList();
        //this.previousPosition = this.transform.position;
    }

    private void FixedUpdate()
    {
        //this.velocity = (this.transform.position - this.previousPosition) / Time.deltaTime;
        //this.previousPosition = this.transform.position;

        this.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.direction);
    }

    // Do this late to make sure all camera updates are done already
    private void LateUpdate()
    {
        // Do this every time, as screen size can change, and its a very cheap calculation
        var bl = GUILayerManager.MainCamera.ScreenToWorldPoint(Vector3.zero);
        var tr = GUILayerManager.MainCamera.ScreenToWorldPoint(Screen.width * Vector3.right + Screen.height * Vector3.up);
        var worldSize = tr - bl;
        this.transform.localScale = Vector3.one * Mathf.Max(worldSize.x, worldSize.y) / this.effectBaseSize;

        foreach(var effect in this.effects)
        {
            effect.Update((0.5f + this.amount) * this.effectMultiplier, this.amount, this.turningAmount);
        }
    }
}
