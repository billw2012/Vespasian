using System;
using UnityEngine;
using System.Collections.Generic;

public class GoodingSolverF {

    /*
     * R1 = position at t0
     * V1 = velocity at t0
     * R2 = position at t1
     * V2 = velocity at t1
     * tof  = time of flight (t1 - t0) (+ posigrade "shortway", - retrograde "longway")
     * nrev = number of full revolutions (+ left-branch, - right-branch for nrev != 0)
     * Vi = initial velocity vector of transfer orbit (Vi - V1 = deltaV)
     * Vf = final velocity vector of transfer orbit (V2 - Vf = deltaV)
     */
    public static void Solve(float GM, Vector2 R1, Vector2 V1, Vector2 R2, Vector2 V2, float tof, int nrev, out Vector2 Vi, out Vector2 Vf) {
        /* most of this function lifted from https://www.mathworks.com/matlabcentral/fileexchange/39530-lambert-s-problem/content/glambert.m */

        // if we don't catch this edge condition, the solver will spin forever (internal state will NaN and produce great sadness)
        if ( tof == 0 )
            throw new Exception( "MechJeb's Gooding Lambert Solver does not support zero time of flight (teleportation)" );

        float VR11, VT11, VR12, VT12;
        float VR21, VT21, VR22, VT22;
        int n;

        // initialize in case we throw
        Vi = Vector2.zero;
        Vf = Vector2.zero;

        //Vector2 ur1xv1 = Vector2.Cross(R1, V1).normalized;

        Vector2 ux1 = R1.normalized;
        Vector2 ux2 = R2.normalized;

        //Vector2 uz1 = Vector2.Cross(ux1, ux2).normalized;

        /* calculate the minimum transfer angle (radians) */

        float theta = Mathf.Acos(Mathf.Clamp(Vector2.Dot(ux1, ux2), -1, 1));

        /* calculate the angle between the orbit normal of the initial orbit and the fundamental reference plane */

        // float angle_to_on = Mathf.Acos(Mathf.Clamp(Vector2.Dot(ur1xv1, uz1), -1, 1));
        //
        // /* if angle to orbit normal is greater than 90 degrees and posigrade orbit, then flip the orbit normal and the transfer angle */
        //
        // if ((angle_to_on > 0.5 * Mathf.PI) && (tof > 0f)) {
        //     theta = 2f * Mathf.PI - theta;
        //     uz1 = -uz1;
        // }
        //
        // if ((angle_to_on < 0.5 * Mathf.PI) && (tof < 0f)) {
        //     theta = 2f * Mathf.PI - theta;
        //     uz1 = -uz1;
        // }
        //
        // Vector2 uz2 = uz1;
        //
        // Vector2 uy1 = Vector2.Cross(uz1, ux1).normalized;
        //
        // Vector2 uy2 = Vector2.Cross(uz2, ux2).normalized;

        theta = theta + 2f * Mathf.PI * Mathf.Abs(nrev);

        VLAMB(GM, R1.magnitude, R2.magnitude, theta, tof, out n, out VR11, out VT11, out VR12, out VT12, out VR21, out VT21, out VR22, out VT22);

        //Debug.Log("VLAMBOUT: n= " + n + " VR11= " + VR11 + " VT11= " + VT11 + " VR12= " + VR12 + " VT12= " + VT12 + " VR21= " + VR21 + " VT21= " + VT21 + " VR22= " + VR22 + " VT22= " + VT22);

        if (nrev > 0) {
            if (n == -1) {
                throw new Exception("Gooding Solver found no tminimum");
            } else if (n == 0) {
                throw new Exception("Gooding Solver found no solution time");
            }
        }

        /* compute transfer orbit initial and final velocity vectors */

        var uy1 = Vector2.Perpendicular(ux1);
        var uy2 = Vector2.Perpendicular(ux2);
        if ((nrev > 0) && (n > 1)) {
            Vi = VR21 * ux1 + VT21 * uy1;
            Vf = VR22 * ux2 + VT22 * uy2;
        } else {
            Vi = VR11 * ux1 + VT11 * uy1;
            Vf = VR12 * ux2 + VT12 * uy2;
        }
    }

    /*
     * Goodings Method
     *
     * MMMMMmmmmmm..... Smells like Fortran....
     *
     * Shield your eyes lest ye be blinded by goto statements...
     *
     * Keep in mind that Gooding optimized the math to reduce loss of precision so "cleaning up" these functions without knowing the values
     * that the variables typically hold could result in two very small or very large numbers being multiplied together and resultant loss
     * of precision, and that rearrangement will make it incredibly difficult to spot simple transcription typos.  It has been deliberately
     * kept as super-fugly looking C# code for those reasons.
     */

    private static void VLAMB(float GM, float R1, float R2, float TH, float TDELT,
            out int N, out float VR11, out float VT11, out float VR12, out float VT12, out float VR21, out float VT21, out float VR22, out float VT22) {

        //Debug.Log("GM= " + GM + " R1= " + R1 + " R2= " + R2 + " TH= " + TH + " TDELT= " + TDELT);
        VR11 = VT11 = VR12 = VT12 = 0f;
        VR21 = VT21 = VR22 = VT22 = 0f;
        int M = Convert.ToInt32(Mathf.Floor(TH / (2f * Mathf.PI)));
        float THR2 = TH / 2f - M * Mathf.PI;
        float DR = R1 - R2;
        float R1R2 = R1 * R2;
        float R1R2TH = 4f * R1R2 * Mathf.Pow(Mathf.Sin(THR2), 2);
        float CSQ = Mathf.Pow(DR, 2) + R1R2TH;
        float C = Mathf.Sqrt(CSQ);
        float S = (R1 + R2 + C) / 2f;
        float GMS = Mathf.Sqrt(GM * S / 2f);
        float QSQFM1 = C / S;
        float Q = Mathf.Sqrt(R1R2) * Mathf.Cos(THR2) / S;
        float RHO;
        float SIG;
        if ( C != 0f ) {
            RHO = DR / C;
            SIG = R1R2TH / CSQ;
        } else {
            RHO = 0f;
            SIG = 1f;
        }
        float T = 4f * GMS * TDELT / Mathf.Pow(S, 2);

        float X1;
        float X2;

        XLAMB(M, Q, QSQFM1, T, out N, out X1, out X2);

        for(int I=1; I <= N; I++) {
            float X;
            if (I == 1)
                X = X1;
            else
                X = X2;

            float QZMINX;
            float QZPLX;
            float ZPLQX;
            float UNUSED;

            TLAMB(M, Q, QSQFM1, X, -1, out UNUSED, out QZMINX, out QZPLX, out ZPLQX);

            float VT2 = GMS * ZPLQX * Mathf.Sqrt(SIG);
            float VR1 = GMS * ( QZMINX - QZPLX * RHO ) / R1;
            float VT1 = VT2 / R1;
            float VR2 = - GMS * ( QZMINX + QZPLX * RHO ) / R2;
            VT2 = VT2 / R2;

            if (I == 1) {
                VR11 = VR1;
                VT11 = VT1;
                VR12 = VR2;
                VT12 = VT2;
            } else {
                VR21 = VR1;
                VT21 = VT1;
                VR22 = VR2;
                VT22 = VT2;
            }
        }
    }

    private static void XLAMB(int M, float Q, float QSQFM1, float TIN, out int N, out float X, out float XPL) {
        float TOL = 3e-7f;
        float C0 = 1.7f;
        float C1 = 0.5f;
        float C2 = 0.03f;
        float C3 = 0.15f;
        float C41 = 1f;
        float C42 = 0.24f;
        float THR2 = Mathf.Atan2(QSQFM1, 2f*Q)/Mathf.PI;
        float T, T0, DT, D2T, D3T;
        float D2T2 = 0f;
        float TMIN = 0f;
        float TDIFF;
        float TDIFFM = 0f;
        float XM = 0f;
        float W;
        X = 0f;
        XPL = 0f;
        if (M == 0) {
            /* "SINGLE-REV STARTER FROM T (AT X = 0) & BILINEAR (USUALLY)" -- Gooding */
            N = 1;
            TLAMB(M, Q, QSQFM1, 0f, 0, out T0, out DT, out D2T, out D3T);
            TDIFF = TIN - T0;
            if (TDIFF <= 0f) {
                X = T0*TDIFF/(-4f*TIN);
                /* "-4 IS THE VALUE OF DT, FOR X = 0" -- Gooding */
            } else {
                X = -TDIFF/(TDIFF + 4f);
                W = X + C0*Mathf.Sqrt(2f*(1f - THR2));
                if (W < 0f)
                    X = X - Mathf.Sqrt(Mathf.Pow(-W, 1f / 8f)) * (X + Mathf.Sqrt(TDIFF/(TDIFF + 1.5f*T0)));
                W = 4f / (4f + TDIFF);
                X = X * ( 1f + X * ( C1 * W - C2 * X * Mathf.Sqrt(W)));
            }
        } else {
            /* "WITH MUTIREVS, FIRST GET T(MIN) AS BASIS FOR STARTER */
            XM = 1f / ( 1.5f * (M + 0.5f) * Mathf.PI );
            if (THR2 < 0.5f)
                XM = Mathf.Pow(2f*THR2, 1f/8f)*XM;
            if (THR2 > 0.5f)
                XM = (2f - Mathf.Pow(2f - 2f*THR2, 1f/8f))*XM;
            /* "STARTER FOR TMIN" */
            for(int I=1; I<= 12; I++) {
                TLAMB(M, Q, QSQFM1, XM, 3, out TMIN, out DT, out D2T, out D3T);
                if (D2T == 0f)
                    goto Two;
                float XMOLD = XM;
                XM = XM - DT*D2T / ( D2T*D2T - DT*D3T/2f );
                float XTEST = Mathf.Abs(XMOLD/XM - 1f);
                if (XTEST <= TOL)
                    goto Two;
            }
            N = -1;
            return;
            /* "(BREAK OFF & EXIT IF TMIN NOT LOCATED - SHOULD NEVER HAPPEN)" */
            /* "NOW PROCEED FROM T(MIN) TO FULL STARTER" -- Gooding */
Two:
            TDIFFM = TIN - TMIN;
            if (TDIFFM < 0f) {
                N = 0;
                return;
                /* "EXIT IF NO SOLUTION ALTREADY FROM X(TMIN)" -- Gooding */
            } else if (TDIFFM == 0f) {
                X = XM;
                N = 1;
                return;
                /* "EXIT IF UNIQUE SOLUTION ALREADY FROM X(TMIN) -- Gooding */
            } else {
                N = 3;
                if ( D2T == 0f)
                    D2T = 6f * M * Mathf.PI;
                X = Mathf.Sqrt(TDIFFM/(D2T/2f + TDIFFM/Mathf.Pow(1f - XM, 2f)));
                W = XM + X;
                W = W*4f/(4f + TDIFFM) + Mathf.Pow(1f - W, 2f);
                X = X*(1f - (1f + M + C41 * (THR2 - 0.5f))/(1f + C3*M) * X * (C1*W + C2*X*Mathf.Sqrt(W))) + XM;
                D2T2 = D2T/2f;
                if (X >= 1f) {
                    N = 1;
                    goto Three;
                }
                /* "(NO FINITE SOLUTION WITH X > XM)" -- Gooding */
            }
        }
        /* "(NOW HAVE A STARTER, SO PROCEED BY HALLEY)" -- Gooding */
Five:
        for(int I=1; I<=3; I++) {
            TLAMB(M, Q, QSQFM1, X, 2, out T, out DT, out D2T, out D3T);
            T = TIN - T;
            if (DT != 0f)
                X = X + T*DT/(DT*DT + T*D2T/2f);
        }

        if (N != 3)
            return;
        /* "(EXIT IF ONLY ONE SOLUTION, NORMALLY WHEN M = 0)" */

        N = 2;
        XPL = X;
Three:
        /* "(SECOND MULTI-REV STARTER)" */
        TLAMB(M, Q, QSQFM1, 0f, 0, out T0, out DT, out D2T, out D3T);

        float TDIFF0 = T0 - TMIN;
        TDIFF = TIN - T0;
        if (TDIFF <= 0f) {
            X = XM - Mathf.Sqrt(TDIFFM/(D2T2 - TDIFFM*(D2T2/TDIFF0 - 1f/Mathf.Pow(XM, 2))));
        } else {
            X = - TDIFF / ( TDIFF + 4f );
            W = X + C0 * Mathf.Sqrt(2f*(1f - THR2));
            if (W < 0f)
                X = X - Mathf.Sqrt(Mathf.Pow(-W, 1f/8f))*(X + Mathf.Sqrt(TDIFF/(TDIFF + 1.5f*T0)));
            W = 4f / (4f + TDIFF);
            X = X*(1f + ( 1f + M + C42*(THR2 - 0.5f))/(1f + C3*M) * X * ( C1*W - C2*X*Mathf.Sqrt(W)));
            if (X <= -1f) {
                N = N - 1;
                /* "(NO FINITE SOLUTION WITH X < XM)" -- Gooding */
                if (N == 1)
                    X = XPL;
            }
        }
        goto Five;

    }

    private static void TLAMB(int M, float Q, float QSQFM1, float X, int N, out float T, out float DT, out float D2T, out float D3T) {
        float SW = 0.4f;
        bool LM1 = N == -1;
        bool L1 = N >= 1;
        bool L2 = N >= 2;
        bool L3 = N == 3;
        float QSQ = Q * Q;
        float XSQ = X * X;
        float U = (1f - X) * (1f + X);
        T = 0f;

        // Yes, we could remove the next test but I added that only to get the compiler to shut up
        DT = 0f;
        D2T = 0f;
        D3T = 0f;

        if (!LM1) {
            /* "NEEDED IF SERIES AND OTHERWISE USEFUL WHEN Z = 0" -- Gooding */
            DT = 0f;
            D2T = 0f;
            D3T = 0f;
        }
        if (LM1 || M > 0f || X < 0f || Mathf.Abs(U) > SW) {
            /* "DIRECT COMPUTATION (NOT SERIES)" -- Gooding */
            float Y = Mathf.Sqrt(Mathf.Abs(U));
            float Z = Mathf.Sqrt(QSQFM1 + QSQ*XSQ);
            float QX = Q * X;

            float A = 0f;
            float B = 0f;
            float AA = 0f;
            float BB = 0f;

            if (QX <= 0f) {
                A = Z - QX;
                B = Q*Z - X;
            }

            if (QX < 0f && LM1) {
                AA = QSQFM1/A;
                BB = QSQFM1 * ( QSQ*U - XSQ ) / B;
            }

            if (QX == 0f && LM1 || QX > 0f ) {
                AA = Z + QX;
                BB = Q*Z + X;
            }

            if ( QX > 0f ) {
                A = QSQFM1 / AA;
                B = QSQFM1 * ( QSQ*U - XSQ ) / BB;
            }

            if (!LM1) {
                float G;
                if (QX * U >= 0f)
                {
                    G = X*Z + Q*U;
                }
                else
                {
                    G = ( XSQ - QSQ*U ) / ( X*Z - Q*U );
                }

                float F = A * Y;
                if ( X <= 1f) {
                    T = M*Mathf.PI + Mathf.Atan2(F, G);
                } else {
                    if (F > SW) {
                        T = Mathf.Log(F + G);
                    } else {
                        float FG1 = F / ( G + 1f);
                        float TERM = 2f * FG1;
                        float FG1SQ = FG1 * FG1;
                        T = TERM;
                        float TWOI1 = 1f;
                        float TOLD;
                        do {
                            TWOI1 = TWOI1 + 2f;
                            TERM = TERM*FG1SQ;
                            TOLD = T;
                            T = T + TERM/TWOI1;
                        } while ( T != TOLD );  /* "CONTINUE LOOPING FOR THE INVERSE TANH" -- Gooding */
                    }
                }
                T = 2f * (T/Y + B) / U;
                if ( L1 && Z != 0f ) {
                    float QZ = Q/Z;
                    float QZ2 = QZ*QZ;
                    QZ = QZ*QZ2;
                    DT = ( 3f*X*T - 4f * ( A + QX*QSQFM1 ) / Z ) / U;
                    if (L2)
                    {
                        D2T = ( 3f*T + 5f*X*DT + 4f*QZ*QSQFM1 ) / U;
                    }
                    if (L3)
                    {
                        D3T = ( 8f*DT + 7f*X*D2T - 12f*QZ*QZ2*X*QSQFM1 ) / U;
                    }
                }
            } else {
                DT = B;
                D2T = BB;
                D3T = AA;
            }
        } else {
            /* "COMPUTE BY SERIES" -- Gooding */
            float U0I = 1f;
            float U1I = 0f;
            float U2I = 0f;
            float U3I = 0f;

            if (L1)
                U1I = 1f;
            if (L2)
                U2I = 1f;
            if (L3)
                U3I = 1f;
            float TERM = 4f;
            float TQ = Q*QSQFM1;
            int I = 0;
            float TQSUM = 0f;
            if (Q < 0.5)
                TQSUM = 1f - Q*QSQ;
            if (Q >= 0.5)
                TQSUM = ( 1f / (1f + Q) + Q ) * QSQFM1;
            float TTMOLD = TERM/3f;
            T = TTMOLD*TQSUM;
            float TOLD;
            do {
                I++;
                int P = I;
                U0I = U0I*U;
                if (L1 && I > 1)
                    U1I = U1I * U;
                if (L2 && I > 2)
                    U2I = U2I * U;
                if (L3 && I > 3)
                    U3I = U3I * U;
                TERM = TERM * ( P - 0.5f ) / P;
                TQ = TQ * QSQ;
                TQSUM = TQSUM + TQ;
                TOLD = T;
                float TTERM = TERM / (2f*P + 3f);
                float TQTERM = TTERM*TQSUM;
                T = T - U0I * (( 1.5f*P + 0.25f ) * TQTERM / ( P*P - 0.25f ) - TTMOLD*TQ );
                TTMOLD = TTERM;
                TQTERM = TQTERM*P;
                if (L1)
                    DT = DT + TQTERM*U1I;
                if (L2)
                    D2T = D2T + TQTERM*U2I * (P - 1f);
                if (L3)
                    D3T = D3T + TQTERM*U3I * (P - 1f) * (P - 2f);
            } while (I < N || T != TOLD);
            if (L3)
                D3T = 8f*X * ( 1.5f*D2T - XSQ*D3T );
            if (L2)
                D2T = 2f * ( 2f*XSQ*D2T - DT );
            if (L1)
                DT = -2f * X * DT;
            T = T/XSQ;
        }
    }

    public static void DebugLogList(List<float> l)
    {
        int i = 0;
        string str = "";
        for(int n1 = 0; n1 < l.Count; n1++) {
            str += String.Format("{0:F8}", l[n1]);
            if (i % 6 == 5)
            {
                Debug.Log(str);
                str = "";
            }
            else
            {
                str += " ";
            }
            i++;
        }
    }

    // sma is positive for elliptical, negative for hyperbolic and is the radius of periapsis for parabolic
    public static void Test(float sma, float ecc) {
        float k = Mathf.Sqrt(Mathf.Abs(1 / (sma*sma*sma)));
        List<float> Elist = new List<float>(); // eccentric anomaly
        List<float> tlist = new List<float>(); // time of flight
        List<float> rlist = new List<float>(); // magnitude of r
        List<float> vlist = new List<float>(); // mangitude of v
        List<float> flist = new List<float>(); // true anomaly
        List<float> dlist = new List<float>(); // list of diffs

        for(float E = 0f; E < 2 * Mathf.PI; E += Mathf.PI / 180f) {
            Elist.Add(E);
            float tof = 0;
            if (ecc < 1 )
                tof = ( E - ecc * Mathf.Sin(E) ) / k;
            else if (ecc == 1)
                tof = Mathf.Sqrt(2) * ( E + E * E * E / 3f ) / k;
            else
                tof = (float) (( ecc * Math.Sinh(E) - E ) / k);

            tlist.Add( tof );

            float smp = 0;
            if ( ecc == 1 )
                smp = 2 * sma;
            else
                smp = sma * ( 1f - ecc * ecc );

            float energy = 0;
            if (ecc != 1)
                energy = -1f / ( 2f * sma );

            float f = 0;
            if (ecc < 1)
                f = 2f * Mathf.Atan(Mathf.Sqrt((1 + ecc) / (1 - ecc)) * Mathf.Tan(E / 2f));
            else if (ecc == 1)
                f = 2 * Mathf.Atan(E);
            else
                f = 2f * Mathf.Atan((float) (Mathf.Sqrt((ecc + 1) / (ecc - 1)) * Math.Tanh(E / 2f)));

            float r = smp / ( 1f + ecc * Mathf.Cos(f));

            float v = Mathf.Sqrt(2 * (energy + 1f / r ) );
            if ( f < 0 )
                f += 2 * Mathf.PI;

            rlist.Add(r);
            vlist.Add(v);
            flist.Add(f);
        }

        float diffmax = 0f;
        int maxn1 = 0;
        int maxn2 = 0;

        for(int n1 = 0; n1 < Elist.Count; n1++) {
            for(int n2 = n1+1; n2 < Elist.Count; n2++) {
                float VR11, VT11, VR12, VT12;
                float VR21, VT21, VR22, VT22;
                int n;

                VLAMB(1f, rlist[n1], rlist[n2], flist[n2] - flist[n1], tlist[n2] - tlist[n1], out n, out VR11, out VT11, out VR12, out VT12, out VR21, out VT21, out VR22, out VT22);
                float Vi = Mathf.Sqrt(VR11 * VR11 + VT11 * VT11);
                float Vf = Mathf.Sqrt(VR12 * VR12 + VT12 * VT12);
                float diff1 = vlist[n1] - Vi;
                float diff2 = vlist[n2] - Vf;
                float diff = Mathf.Sqrt(diff1 * diff1 + diff2 * diff2);
                dlist.Add(diff);
                if (diff > diffmax)
                {
                    diffmax = diff;
                    maxn1 = n1;
                    maxn2 = n2;
                }
            }
        }
        //DebugLogList(dlist);

        Debug.Log("diffmax = " + diffmax + " n1 = " + maxn1 + " n2 = " + maxn2);
    }
}
