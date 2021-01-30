/// <summary>
/// SURGE FRAMEWORK
/// Author: Bob Berkebile
/// Email: bobb@pixelplacement.com
/// 
/// Renders a spline by interfacing with an attached LineRenderer.
/// 
/// </summary>

using UnityEngine;

namespace Pixelplacement
{
    [ExecuteInEditMode]
    [RequireComponent (typeof (LineRenderer))]
    [RequireComponent (typeof (Spline))]
    public class SplineRenderer : MonoBehaviour
    {
        //Public Variables:
        public int segmentsPerCurve = 25;
        [Range (0,1)] public float startPercentage;
        [Range (0,1)] public float endPercentage = 1;

        //Private Variables:
        private LineRenderer _lineRenderer;
        private Spline _spline;
        private bool _initialized;
        private int _previousAnchorsLength;
        private int _previousSegmentsPerCurve;
        private int _vertexCount;
        private float _previousStart;
        private float _previousEnd;

        //Init:
        private void Reset ()
        {
            _lineRenderer = GetComponent<LineRenderer> ();

            _initialized = false;

            _lineRenderer.startWidth = .03f;
            _lineRenderer.endWidth = .03f;
            _lineRenderer.startColor = Color.white;
            _lineRenderer.endColor = Color.yellow;

            _lineRenderer.material = Resources.Load("SplineRenderer") as Material;
        }

        //Loop:
        private void Update ()
        {
            //initialize:
            if (!_initialized)
            {
                //refs:
                _lineRenderer = GetComponent<LineRenderer> ();
                _spline = GetComponent<Spline> ();

                //initial setup:
                ConfigureLineRenderer ();
                UpdateLineRenderer ();

                _initialized = true;
            }

            //configure line renderer:
            if (segmentsPerCurve != _previousSegmentsPerCurve || _previousAnchorsLength != _spline.Anchors.Length)
            {
                ConfigureLineRenderer ();
                UpdateLineRenderer ();
            }

            if (_spline.Anchors.Length <= 1)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            //if any part of the spline is changed update line renderer:
            foreach (var item in _spline.Anchors)
            {
                if (item.RenderingChange)
                {
                    item.RenderingChange = false;
                    UpdateLineRenderer ();
                }
            }

            //if the range has changed, update:
            if (startPercentage != _previousStart || endPercentage != _previousEnd)
            {
                UpdateLineRenderer ();

                //reset:
                _previousStart = startPercentage;
                _previousEnd = endPercentage;
            }
        }

        //Private Methods:
        private void UpdateLineRenderer ()
        {
            if (_spline.Anchors.Length < 2) return;
            for (int i = 0; i < _vertexCount; i++)
            {
                float percentage = i/(float)(_vertexCount - 1);
                float sample = Mathf.Lerp (startPercentage, endPercentage, percentage);
                _lineRenderer.SetPosition (i, _spline.GetPosition(sample, false));
            }
        }

        private void ConfigureLineRenderer ()
        {
            segmentsPerCurve = Mathf.Max (0, segmentsPerCurve);
            _vertexCount = (segmentsPerCurve * (_spline.Anchors.Length - 1)) + 2;
            if (Mathf.Sign (_vertexCount) == 1) _lineRenderer.positionCount = _vertexCount;
            _previousSegmentsPerCurve = segmentsPerCurve;
            _previousAnchorsLength = _spline.Anchors.Length;
        }
    }
}