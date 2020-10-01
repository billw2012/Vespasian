using UnityEditor;
using UnityEngine;

public class CameraPostEffectRadius : MonoBehaviour
{
    [Range(0, 20)]
    public float innerRadius = 1;
    [Range(0, 20)]
    public float falloffRange = 3;

    public Transform cameraTransform;

    public PostEffect effect;

    // Start is called before the first frame update
    void Start()
    {
        if(this.cameraTransform == null)
        {
            this.cameraTransform = Camera.main.transform;
        }

        this.effect.Init();
    }

    bool wasInRange = false;
    // Update is called once per frame
    void Update()
    {
        float distance = ((Vector2)this.transform.worldToLocalMatrix.MultiplyPoint(this.cameraTransform.position)).magnitude;
        bool inRange = distance < this.innerRadius + this.falloffRange;
        // This makes sure we only update when we are in range, and once to completely revert them
        // when we move out of range
        if (this.wasInRange || inRange)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(this.innerRadius + this.falloffRange, this.innerRadius, distance));
            this.effect.Update(t);
        }
        this.wasInRange = inRange;
    }

    void OnDestroy()
    {
        this.effect.ResetSettings();
    }


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = UnityEngine.Color.yellow;
        Handles.matrix = this.transform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.innerRadius);
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.innerRadius + this.falloffRange);
        GUIUtils.Label(Vector3.up * this.innerRadius, "Postfx");
    }
#endif
}
