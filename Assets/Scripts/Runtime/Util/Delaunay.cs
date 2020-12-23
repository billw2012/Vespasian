/**
 * Copyright 2019 Oskar Sigvardsson
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GK
{
    public static class Geom
    {

        /// <summary>
        /// Are these two vectors (approximately) coincident
        /// </summary>
        public static bool AreCoincident(Vector2 a, Vector2 b)
        {
            return (a - b).magnitude < 0.000001f;
        }

        /// <summary>
        /// Is point p to the left of the line from l0 to l1?
        /// </summary>
        public static bool ToTheLeft(Vector2 p, Vector2 l0, Vector2 l1)
        {
            return (l1.x - l0.x) * (p.y - l0.y) - (l1.y - l0.y) * (p.x - l0.x) >= 0;
        }

        /// <summary>
        /// Is point p to the right of the line from l0 to l1?
        /// </summary>
        public static bool ToTheRight(Vector2 p, Vector2 l0, Vector2 l1)
        {
            return !ToTheLeft(p, l0, l1);
        }

        /// <summary>
        /// Is point p inside the triangle formed by c0, c1 and c2 (assuming c1,
        /// c2 and c3 are in CCW order)
        /// </summary>
        public static bool PointInTriangle(Vector2 p, Vector2 c0, Vector2 c1, Vector2 c2)
        {
            return ToTheLeft(p, c0, c1)
                && ToTheLeft(p, c1, c2)
                && ToTheLeft(p, c2, c0);
        }

        /// <summary>
        /// Is point p inside the circumcircle formed by c0, c1 and c2?
        /// </summary>
        public static bool InsideCircumcircle(Vector2 p, Vector2 c0, Vector2 c1, Vector2 c2)
        {
            float ax = c0.x - p.x;
            float ay = c0.y - p.y;
            float bx = c1.x - p.x;
            float by = c1.y - p.y;
            float cx = c2.x - p.x;
            float cy = c2.y - p.y;

            return (ax * ax + ay * ay) * (bx * cy - cx * @by) -
                (bx * bx + @by * @by) * (ax * cy - cx * ay) +
                (cx * cx + cy * cy) * (ax * @by - bx * ay) > 0.000001f;
        }

        /// <summary>
        /// Rotate vector v left 90 degrees
        /// </summary>
        public static Vector2 RotateRightAngle(Vector2 v)
        {
            float x = v.x;
            v.x = -v.y;
            v.y = x;

            return v;
        }

        /// <summary>
        /// General line/line intersection method. Each line is defined by a
        /// two vectors, a point on the line (p0 and p1 for the two lines) and a
        /// direction vector (v0 and v1 for the two lines). The returned value
        /// indicates whether the lines intersect. m0 and m1 are the
        /// coefficients of how much you have to multiply the direction vectors
        /// to get to the intersection. 
        ///
        /// In other words, if the intersection is located at X, then: 
        ///
        ///     X = p0 + m0 * v0
        ///     X = p1 + m1 * v1
        ///
        /// By checking the m0/m1 values, you can check intersections for line
        /// segments and rays.
        /// </summary>
        public static bool LineLineIntersection(Vector2 p0, Vector2 v0, Vector2 p1, Vector2 v1, out float m0, out float m1)
        {
            float det = v0.x * v1.y - v0.y * v1.x;

            if (Mathf.Abs(det) < 0.001f)
            {
                m0 = float.NaN;
                m1 = float.NaN;

                return false;
            }
            else
            {
                m0 = ((p0.y - p1.y) * v1.x - (p0.x - p1.x) * v1.y) / det;

                m1 = Mathf.Abs(v1.x) >= 0.001f ? (p0.x + m0 * v0.x - p1.x) / v1.x : (p0.y + m0 * v0.y - p1.y) / v1.y;

                return true;
            }
        }

        /// <summary>
        /// Returns the intersections of two lines. p0/p1 are points on the
        /// line, v0/v1 are the direction vectors for the lines. 
        ///
        /// If there are no intersections, returns a NaN vector
        /// <summary>
        public static Vector2 LineLineIntersection(Vector2 p0, Vector2 v0, Vector2 p1, Vector2 v1)
        {
            return LineLineIntersection(p0, v0, p1, v1, out float m0, out _) ? p0 + m0 * v0 : new Vector2(float.NaN, float.NaN);
        }

        /// <summary>
        /// Returns the center of the circumcircle defined by three points (c0,
        /// c1 and c2) on its edge.
        /// </summary>
        public static Vector2 CircumcircleCenter(Vector2 c0, Vector2 c1, Vector2 c2)
        {
            var mp0 = 0.5f * (c0 + c1);
            var mp1 = 0.5f * (c1 + c2);

            var v0 = RotateRightAngle(c0 - c1);
            var v1 = RotateRightAngle(c1 - c2);

            Geom.LineLineIntersection(mp0, v0, mp1, v1, out float m0, out _);

            return mp0 + m0 * v0;
        }

        /// <summary>
        /// Returns the triangle centroid for triangle defined by points c0, c1
        /// and c2. 
        /// </summary>
        public static Vector2 TriangleCentroid(Vector2 c0, Vector2 c1, Vector2 c2)
        {
            var val = 1.0f / 3.0f * (c0 + c1 + c2);
            return val;
        }

        /// <summary>
        /// Returns the signed area of a polygon. CCW polygons return a positive
        /// area, CW polygons return a negative area.
        /// </summary>
        public static float Area(IList<Vector2> polygon)
        {
            float area = 0.0f;

            int count = polygon.Count;

            for (int i = 0; i < count; i++)
            {
                int j = i == count - 1 ? 0 : i + 1;

                var p0 = polygon[i];
                var p1 = polygon[j];

                area += p0.x * p1.y - p1.y * p1.x;
            }

            return 0.5f * area;
        }
    }

    public class DelaunayTriangulation
    {

        /// <summary>
        /// List of vertices that make up the triangulation
        /// </summary>
        public readonly List<Vector2> Vertices;

        /// <summary>
        /// List of triangles that make up the triangulation. The elements index
        /// the Vertices array. 
        /// </summary>
        public readonly List<int> Triangles;

        internal DelaunayTriangulation()
        {
            this.Vertices = new List<Vector2>();
            this.Triangles = new List<int>();
        }

        internal void Clear()
        {
            this.Vertices.Clear();
            this.Triangles.Clear();
        }

        /// <summary>
        /// Verify that this is an actual Delaunay triangulation
        /// </summary>
        public bool Verify()
        {
            try
            {
                for (int i = 0; i < this.Triangles.Count; i += 3)
                {
                    var c0 = this.Vertices[this.Triangles[i]];
                    var c1 = this.Vertices[this.Triangles[i + 1]];
                    var c2 = this.Vertices[this.Triangles[i + 2]];

                    for (int j = 0; j < this.Vertices.Count; j++)
                    {
                        var p = this.Vertices[j];
                        if (Geom.InsideCircumcircle(p, c0, c1, c2))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class DelaunayCalculator
    {
        private int highest = -1;
        private IList<Vector2> verts;
        private readonly List<int> indices;
        private readonly List<TriangleNode> triangles;

        /// <summary>
        /// Creates a new Delaunay triangulation calculator
        /// </summary>
        public DelaunayCalculator()
        {
            this.triangles = new List<TriangleNode>();
            this.indices = new List<int>();
        }

        /// <summary>
        /// Calculate the triangulation of the supplied vertices
        /// </summary>
        /// <param name="verts">List of vertices to use for calculation</param>
        /// <returns>The calculated Delaunay triangulation<returns>
        public static DelaunayTriangulation CalculateTriangulation(IList<Vector2> verts)
        {
            var calculator = new DelaunayCalculator();

            DelaunayTriangulation result = null;
            calculator.CalculateTriangulation(verts, ref result);
            return result;
        }

        /// <summary>
        /// Calculate the triangulation of the supplied vertices.
        ///
        /// This overload allows you to reuse the result object, to prevent
        /// garbage from being created.
        /// </summary>
        /// <param name="verts">List of vertices to use for calculation</param>
        /// <param name="result">Result object to store the triangulation in</param>
        public void CalculateTriangulation(IList<Vector2> verts, ref DelaunayTriangulation result)
        {
            if (verts == null)
            {
                throw new ArgumentNullException("points");
            }
            if (verts.Count < 3)
            {
                throw new ArgumentException("You need at least 3 points for a triangulation");
            }

            this.triangles.Clear();
            this.verts = verts;

            this.highest = 0;

            for (int i = 0; i < verts.Count; i++)
            {
                if (this.Higher(this.highest, i))
                {
                    this.highest = i;
                }
            }

            //ShuffleIndices();

            // Add first triangle, the bounding triangle.
            this.triangles.Add(new TriangleNode(-2, -1, this.highest));

            this.RunBowyerWatson();
            this.GenerateResult(ref result);

            this.verts = null;
        }

        private bool Higher(int pi0, int pi1)
        {
            if (pi0 == -2)
            {
                return false;
            }
            else if (pi0 == -1)
            {
                return true;
            }
            else if (pi1 == -2)
            {
                return true;
            }
            else if (pi1 == -1)
            {
                return false;
            }
            else
            {
                var p0 = this.verts[pi0];
                var p1 = this.verts[pi1];

                return p0.y < p1.y || p0.y <= p1.y && p0.x < p1.x;
            }
        }

        /// <summary>
        /// Run the algorithm
        /// </summary>
        private void RunBowyerWatson()
        {
            // For each point, find the containing triangle, split it into three
            // new triangles, call LegalizeEdge on all edges opposite the newly
            // inserted points
            for (int i = 0; i < this.verts.Count; i++)
            {
                //var pi = indices[i];
                int pi = i;

                if (pi == this.highest) continue;

                // Index of the containing triangle
                int ti = this.FindTriangleNode(pi);

                var t = this.triangles[ti];

                // The points of the containing triangle in CCW order
                int p0 = t.P0;
                int p1 = t.P1;
                int p2 = t.P2;

                // Indices of the newly created triangles.
                int nti0 = this.triangles.Count;
                int nti1 = nti0 + 1;
                int nti2 = nti0 + 2;

                // The new triangles! All in CCW order
                var nt0 = new TriangleNode(pi, p0, p1);
                var nt1 = new TriangleNode(pi, p1, p2);
                var nt2 = new TriangleNode(pi, p2, p0);


                // Setting the adjacency triangle references.  Only way to make
                // sure you do this right is by drawing the triangles up on a
                // piece of paper.
                nt0.A0 = t.A2;
                nt1.A0 = t.A0;
                nt2.A0 = t.A1;

                nt0.A1 = nti1;
                nt1.A1 = nti2;
                nt2.A1 = nti0;

                nt0.A2 = nti2;
                nt1.A2 = nti0;
                nt2.A2 = nti1;

                // The new triangles are the children of the old one.
                t.C0 = nti0;
                t.C1 = nti1;
                t.C2 = nti2;

                this.triangles[ti] = t;

                this.triangles.Add(nt0);
                this.triangles.Add(nt1);
                this.triangles.Add(nt2);

                if (nt0.A0 != -1) this.LegalizeEdge(nti0, nt0.A0, pi, p0, p1);
                if (nt1.A0 != -1) this.LegalizeEdge(nti1, nt1.A0, pi, p1, p2);
                if (nt2.A0 != -1) this.LegalizeEdge(nti2, nt2.A0, pi, p2, p0);
            }
        }

        /// <summary>
        /// Filter the points array and triangle tree into a readable result.
        /// </summary>
        private void GenerateResult(ref DelaunayTriangulation result)
        {
            if (result == null)
            {
                result = new DelaunayTriangulation();
            }

            result.Clear();

            for (int i = 0; i < this.verts.Count; i++)
            {
                result.Vertices.Add(this.verts[i]);
            }

            for (int i = 1; i < this.triangles.Count; i++)
            {
                var t = this.triangles[i];

                if (t.IsLeaf && t.IsInner)
                {
                    result.Triangles.Add(t.P0);
                    result.Triangles.Add(t.P1);
                    result.Triangles.Add(t.P2);
                }
            }

        }

        /// <summary>
        /// Shuffle the indices array. Optimal runtime depends on shuffled
        /// input.
        /// </summary>
        private void ShuffleIndices()
        {
            this.indices.Clear();
            this.indices.Capacity = this.verts.Count;

            for (int i = 0; i < this.verts.Count; i++)
            {
                this.indices.Add(i);
            }

            Debug.Assert(this.indices.Count == this.verts.Count);

            for (int i = 0; i < this.verts.Count - 1; i++)
            {
                int j = UnityEngine.Random.Range(i, this.verts.Count);

                int tmp = this.indices[i];
                this.indices[i] = this.indices[j];
                this.indices[j] = tmp;
            }
        }

        /// <summary>
        /// Find the leaf of the triangles[ti] subtree that contains a given
        /// edge.
        ///
        /// We need this because when we split or flip triangles, the adjacency
        /// references don't update, so even if the adjacency triangles were
        /// leaves when the node was created, they might not be leaves later.
        /// If they aren't, they're going to be the ancestor of the correct
        /// leaf, so this method goes down the tree finding the right leaf.
        /// </summary>
        private int LeafWithEdge(int ti, int e0, int e1)
        {
            Debug.Assert(this.triangles[ti].HasEdge(e0, e1));

            while (!this.triangles[ti].IsLeaf)
            {
                var t = this.triangles[ti];

                if (t.C0 != -1 && this.triangles[t.C0].HasEdge(e0, e1))
                {
                    ti = t.C0;
                }
                else if (t.C1 != -1 && this.triangles[t.C1].HasEdge(e0, e1))
                {
                    ti = t.C1;
                }
                else if (t.C2 != -1 && this.triangles[t.C2].HasEdge(e0, e1))
                {
                    ti = t.C2;
                }
                else
                {
                    Debug.Assert(false);
                    throw new System.Exception("This should never happen");
                }
            }

            return ti;
        }

        /// <summary>
        /// Is the edge legal, or does it need to be flipped?
        /// </summary>
        private bool LegalEdge(int k, int l, int i, int j)
        {
            Debug.Assert(k != this.highest && k >= 0);

            bool lMagic = l < 0;
            bool iMagic = i < 0;
            bool jMagic = j < 0;

            Debug.Assert(!(iMagic && jMagic));

            if (lMagic)
            {
                return true;
            }
            else if (iMagic)
            {
                Debug.Assert(!jMagic);

                var p = this.verts[l];
                var l0 = this.verts[k];
                var l1 = this.verts[j];

                return Geom.ToTheLeft(p, l0, l1);
            }
            else if (jMagic)
            {
                Debug.Assert(!iMagic);

                var p = this.verts[l];
                var l0 = this.verts[k];
                var l1 = this.verts[i];

                return !Geom.ToTheLeft(p, l0, l1);
            }
            else
            {
                Debug.Assert(k >= 0 && l >= 0 && i >= 0 && j >= 0);

                var p = this.verts[l];
                var c0 = this.verts[k];
                var c1 = this.verts[i];
                var c2 = this.verts[j];

                Debug.Assert(Geom.ToTheLeft(c2, c0, c1));
                Debug.Assert(Geom.ToTheLeft(c2, c1, p));

                return !Geom.InsideCircumcircle(p, c0, c1, c2);
            }
        }

        /// <summary>
        /// Key part of the algorithm. Flips edges if they need to be flipped,
        /// and recurses.
        ///
        /// pi is the newly inserted point, creating a new triangle ti0.
        /// The adjacent triangle opposite pi in ti0 is ti1. The edge separating
        /// the two triangles is li0 and li1.
        ///
        /// Checks if the (li0, li1) edge needs to be flipped. If it does,
        /// creates two new triangles, and recurses to check if the newly
        /// created triangles need flipping.
        /// <summary>
        private void LegalizeEdge(int ti0, int ti1, int pi, int li0, int li1)
        {
            // ti1 might not be a leaf node (ti0 is guaranteed to be, it was
            // just created), so find the current correct leaf.
            ti1 = this.LeafWithEdge(ti1, li0, li1);

            var t0 = this.triangles[ti0];
            var t1 = this.triangles[ti1];
            int qi = t1.OtherPoint(li0, li1);

            Debug.Assert(t0.HasEdge(li0, li1));
            Debug.Assert(t1.HasEdge(li0, li1));
            Debug.Assert(t0.IsLeaf);
            Debug.Assert(t1.IsLeaf);
            Debug.Assert(t0.P0 == pi || t0.P1 == pi || t0.P2 == pi);
            Debug.Assert(t1.P0 == qi || t1.P1 == qi || t1.P2 == qi);


            //var p = points[pi];
            //var q = points[qi];
            //var l0 = points[li0];
            //var l1 = points[li1];

            if (!this.LegalEdge(pi, qi, li0, li1))
            {
                int ti2 = this.triangles.Count;
                int ti3 = ti2 + 1;

                var t2 = new TriangleNode(pi, li0, qi);
                var t3 = new TriangleNode(pi, qi, li1);

                t2.A0 = t1.Opposite(li1);
                t2.A1 = ti3;
                t2.A2 = t0.Opposite(li1);

                t3.A0 = t1.Opposite(li0);
                t3.A1 = t0.Opposite(li0);
                t3.A2 = ti2;

                this.triangles.Add(t2);
                this.triangles.Add(t3);

                var nt0 = this.triangles[ti0];
                var nt1 = this.triangles[ti1];

                nt0.C0 = ti2;
                nt0.C1 = ti3;

                nt1.C0 = ti2;
                nt1.C1 = ti3;

                this.triangles[ti0] = nt0;
                this.triangles[ti1] = nt1;

                if (t2.A0 != -1) this.LegalizeEdge(ti2, t2.A0, pi, li0, qi);
                if (t3.A0 != -1) this.LegalizeEdge(ti3, t3.A0, pi, qi, li1);
            }
        }

        /// <summary>
        /// Find the leaf triangle in the triangle tree containing a certain point.
        /// </summary>
        private int FindTriangleNode(int pi)
        {
            int curr = 0;

            while (!this.triangles[curr].IsLeaf)
            {
                var t = this.triangles[curr];

                curr = t.C0 >= 0 && this.PointInTriangle(pi, t.C0) 
                    ? t.C0
                    : t.C1 >= 0 && this.PointInTriangle(pi, t.C1)
                        ? t.C1
                        : t.C2;
            }

            return curr;
        }

        /// <summary>
        /// Convenience method to check if a point is inside a certain triangle.
        /// </summary>
        private bool PointInTriangle(int pi, int ti)
        {
            var t = this.triangles[ti];
            return this.ToTheLeft(pi, t.P0, t.P1)
                && this.ToTheLeft(pi, t.P1, t.P2)
                && this.ToTheLeft(pi, t.P2, t.P0);
        }

        /// <summary>
        /// Is the point to the left of the edge?
        /// </summary>
        private bool ToTheLeft(int pi, int li0, int li1)
        {
            if (li0 == -2)
            {
                return this.Higher(li1, pi);
            }
            else if (li0 == -1)
            {
                return this.Higher(pi, li1);
            }
            else if (li1 == -2)
            {
                return this.Higher(pi, li0);
            }
            else if (li1 == -1)
            {
                return this.Higher(li0, pi);
            }
            else
            {
                Debug.Assert(li0 >= 0);
                Debug.Assert(li1 >= 0);

                return Geom.ToTheLeft(this.verts[pi], this.verts[li0], this.verts[li1]);
            }
        }

        /// <summary>
        /// A single node in the triangle tree.
        ///
        /// All parameters are indexes.
        /// </summary>
        private struct TriangleNode
        {
            // The points of the triangle
            public int P0;
            public int P1;
            public int P2;

            // The child triangles of this triangle in the tree
            //
            // A value of -1 means "no child"
            public int C0;
            public int C1;
            public int C2;

            // The triangles adjacent to this triangle
            //
            // A0 is the adjacent triangle opposite to the P0 point (i.e. the A0
            // triangle has (P1, P2) as an edge.
            //
            // A value of -1 means "no adjacent triangle" (only true for
            // triangles with one edge on the bounding triangle).
            public int A0;
            public int A1;
            public int A2;

            // Is this a leaf triangle?
            public bool IsLeaf
            {
                get
                {
                    return this.C0 < 0 && this.C1 < 0 && this.C2 < 0;
                }
            }

            /// <summary>
            /// Is this an "inner" triangle, part of the final triangulation, or
            /// is some part of this triangle connected to the bounding triangle.
            /// </summary>
            public bool IsInner
            {
                get
                {
                    return this.P0 >= 0 && this.P1 >= 0 && this.P2 >= 0;
                }
            }

            public TriangleNode(int P0, int P1, int P2)
            {
                this.P0 = P0;
                this.P1 = P1;
                this.P2 = P2;

                this.C0 = -1;
                this.C1 = -1;
                this.C2 = -1;

                this.A0 = -1;
                this.A1 = -1;
                this.A2 = -1;
            }


            /// <summary>
            /// Does this triangle contain this edge?
            /// </summary>
            public bool HasEdge(int e0, int e1)
            {
                if (e0 == this.P0)
                {
                    return e1 == this.P1 || e1 == this.P2;
                }
                else if (e0 == this.P1)
                {
                    return e1 == this.P0 || e1 == this.P2;
                }
                else if (e0 == this.P2)
                {
                    return e1 == this.P0 || e1 == this.P1;
                }

                return false;
            }


            /// <summary>
            /// Assuming p0 and p1 are one of P0 and P1, return the third point.
            /// </summary>
            public int OtherPoint(int p0, int p1)
            {
                if (p0 == this.P0)
                {
                    return p1 == this.P1 ? this.P2 : p1 == this.P2 ? this.P1 : throw new ArgumentException("p0 and p1 not on triangle");
                }
                if (p0 == this.P1)
                {
                    return p1 == this.P0 ? this.P2 : p1 == this.P2 ? this.P0 : throw new ArgumentException("p0 and p1 not on triangle");
                }
                if (p0 == this.P2)
                {
                    return p1 == this.P0 ? this.P1 : p1 == this.P1 ? this.P0 : throw new ArgumentException("p0 and p1 not on triangle");
                }

                throw new ArgumentException("p0 and p1 not on triangle");
            }


            /// <summary>
            /// Get the triangle opposite a certain point.
            /// </summary>
            public int Opposite(int p)
            {
                if (p == this.P0) return this.A0;
                return p == this.P1 ? this.A1 : p == this.P2 ? this.A2 : throw new ArgumentException("p not in triangle");
            }

            /// <summary>
            /// For debugging purposes.
            /// </summary>
            public override string ToString()
            {
                return this.IsLeaf
                    ? string.Format("TriangleNode({0}, {1}, {2})", this.P0, this.P1, this.P2)
                    : string.Format("TriangleNode({0}, {1}, {2}, {3}, {4}, {5})", this.P0, this.P1, this.P2, this.C0, this.C1, this.C2);
            }
        }
    }
}