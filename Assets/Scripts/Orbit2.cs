using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


// Simulated orbit with baking
// Uses:
// - elliptical orbit of body -- baked
// - ballistic trajectory -- baked for specified range (in time), then simulated

// bodies:
// - sun/planet/moon/comet -- one gravity source effects it, elliptical orbit -- can be baked
// - player -- no parent, can exert force -- needs to be simulated to show lookahead path, but not baked
// - parabolic trajectory -- 
[Serializable]
public struct OrbitParameters2
{
    public float distance;
    public float angle;
    public float angularVelocity;

    public float approxOrbitTime => 360f / this.angularVelocity;

    public struct OrbitPath
    {
        public Vector3[] path;
        public float dt;

        public Vector3 GetPosition(float t)
        {
            float fIdx = (t / this.dt) % this.path.Length;
            int idx0 = Mathf.FloorToInt(fIdx);
            int idx1 = Mathf.CeilToInt(fIdx) % this.path.Length;
            float frac = fIdx - idx0;
            return Vector3.Lerp(this.path[idx0], this.path[idx1], frac);
        }
    }


    public OrbitPath CalculatePath(float parentMass, float gravitationalConstant, float pathQuality)
    {
        var orbit = new OrbitPhysics(this.distance, this.angle, this.angularVelocity, parentMass, gravitationalConstant);

        const int SimSteps = 100;
        //float dt = Time.fixedDeltaTime / SimSteps;
        float dt = pathQuality * this.approxOrbitTime / (10f * 360f);
        var pathList = new List<Vector3>();
        // float lastAngle = orbit.angle - 1f;

        for(int itr = 0; itr < 10000 && orbit.angle < this.angle + 360f; ++itr)
        {
            var pos = orbit.GetPosition();
            // Avoid broken positions
            if (pos.magnitude < 1000)
            {
                pathList.Add(pos);
            }
            for (int step = 0; step < SimSteps; step++)
            {
                orbit.Step(dt / SimSteps);
            }
        }

        if(orbit.angle >= this.angle + 360f)
        {
            var diff = pathList.First() - pathList.Last();
            for (int i = 0; i < pathList.Count; i++)
            {
                pathList[i] += diff * i / pathList.Count;
            }
        }

        //var secondHalf = pathList.AsEnumerable()
        //    .Reverse()
        //    .Select(p => Vector3.Reflect(p, Vector2.Perpendicular(pathList[0]).normalized));
        //Assert.AreNotEqual(orbit.angle, lastAngle, $"Failed to make process in CalculatePath");
        //return new OrbitPath { path = pathList.Concat(secondHalf).ToArray(), dt = dt};
        return new OrbitPath { path = pathList.ToArray(), dt = dt };
    }
}

public class Orbit2 : MonoBehaviour
{
    public OrbitParameters2 parameters = new OrbitParameters2
    {
        distance = 5,
        angle = 0,
        angularVelocity = 1
    };

    public GameConstants constants;

    [Tooltip("How finely subdivided the path rendering is"), Range(0, 1)]
    public float pathQuality = 1;

    [Tooltip("Transform to apply the position to, defaults to any child called 'Position'")]
    public Transform customPosition;
    [HideInInspector]
    public Transform position;

    [Tooltip("Whether to automatically adjust angularVelocity orbit parameter based on distance")]
    public bool autoAngularVelocity = true;
    [Tooltip("Whether automatically motion is clockwise")]
    public bool clockwiseMotion = false;

    public OrbitParameters2.OrbitPath orbitPath;

    public Vector3[] pathPositions => this.orbitPath.path;

    // This was used to more closely match the SimManager math by simply adding positions to get world location for
    // nested orbits. However it doesn't appear to be necessary and can be removed at a later time if the path
    // sim proves stable.
    //Orbit parent;

    void OnValidate()
    {
        this.RefreshValidateRecursive();
    }

    void Start()
    {
        this.RefreshValidate();
        this.CreateOrbitPath();
    }

    void Update()
    {
        this.UpdateOrbitWidth();
    }

    //float LawOfPeriods(float parentMass, float meanDistance)
    //{
    //    return Mathf.Sqrt(4f * Mathf.Pow(Mathf.PI, 2f) * Mathf.Pow(meanDistance, 3f) / (parentMass * this.constants.GravitationalConstant));
    //}

    float FindParentsMass()
    {
        var directParent = this.gameObject.GetComponentInParentOnly<GravitySource>();
        if (directParent != null)
            return directParent.parameters.mass;
        // Instead use sum of sun masses...
        return GameObject.FindObjectsOfType<SunLogic>().Select(s => s.GetComponent<GravitySource>().parameters.mass).Sum();
    }

    public void RefreshValidateRecursive()
    {
        this.RefreshValidate();

        foreach (var child in this.GetComponentsInChildren<Orbit>().Where(c => c != this))
        {
            child.RefreshValidateRecursive();
        }
    }

    void RefreshValidate()
    {
        Assert.IsNotNull(this.constants);

        this.position = this.customPosition == null ? this.transform.Find("Position") : this.customPosition;
        bool ValidateParents()
        {
            // Only game objects being controlled by Orbit component are allowed to have non-identity transforms
            var orbitControlled = GameObject.FindObjectsOfType<Orbit>().Select(o => o.position.gameObject);
            var invalidParents = this.GetAllParents()
                .Where(p => !orbitControlled.Contains(p) && !p.transform.IsIdentity());
            if (invalidParents.Any())
            {
                foreach (var p in invalidParents)
                {
                    Debug.LogError($"{this}: parent {p.name} is invalid, it has none zero transform", p);
                }
                return false;
            }
            return true;
        }

        Debug.Assert(ValidateParents());

        //this.parameters.eccentricity = Mathf.Clamp(this.parameters.eccentricity, 0, 0.3f);

        //if (this.autoMotionPerSecond)
        //{
        //    this.parameters.motionPerSecond = this.parameters.meanDistance > 0?
        //        (this.clockwiseMotion? -360f : 360f) / this.LawOfPeriods(this.FindParentsMass(), this.parameters.meanDistance)
        //        :
        //        0f;
        //}

        this.orbitPath = this.parameters.CalculatePath(this.FindParentsMass(), this.constants.GravitationalConstant, this.pathQuality);

        this.UpdatePosition(0);
    }

    public void SimUpdate()
    {
        this.UpdatePosition(GameLogic.Instance.simTime);
    }

    void UpdatePosition(float time)
    {
        // See note on the parent variable declaration above
        //var newPosition = (Vector3)this.parameters.GetPosition(time);
        //if (this.parent != null)
        //{
        //    newPosition += this.parent.position.position;
        //}
        //this.position.position = newPosition;
        this.position.localPosition = this.orbitPath.GetPosition(time);
    }

    //public Vector3[] GetPositions(float degrees = 360f, float quality = 1)
    //{
    //    degrees = Mathf.Clamp(degrees, -360f, 360f);
    //    int numPoints = Math.Max(1, (int)(Mathf.Abs(degrees) * this.pathQuality * quality));
    //    var path = new Vector3[numPoints];
    //    float timePerPoint = degrees / (numPoints - 1);
    //    for (int i = 0; i < numPoints; i++)
    //    {
    //        path[i] = this.parameters.GetPosition(0, i * timePerPoint);
    //    }
    //    return path;
    //}

    void CreateOrbitPath()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }
        lineRenderer.positionCount = this.pathPositions.Length;
        lineRenderer.SetPositions(this.pathPositions);
    }

    void UpdateOrbitWidth()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }
        lineRenderer.startWidth = lineRenderer.endWidth = this.constants.OrbitLineWidth;
    }

}
