﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADK
{
    /// <summary>
    /// Contains methods for calculating appearance parameters of celestial bodies
    /// </summary>
    public static class Appearance
    {
        /// <summary>
        /// Gets geocentric elongation angle of the celestial body
        /// </summary>
        /// <param name="sun">Ecliptical geocentrical coordinates of the Sun</param>
        /// <param name="body">Ecliptical geocentrical coordinates of the body</param>
        /// <returns>Geocentric elongation angle, in degrees, from -180 to 180.
        /// Negative sign means western elongation, positive eastern.
        /// </returns>
        /// <remarks>
        /// AA(II), formula 48.2
        /// </remarks>
        // TODO: tests
        public static double Elongation(CrdsEcliptical sun, CrdsEcliptical body)
        {
            double beta = Angle.ToRadians(body.Beta);
            double lambda = Angle.ToRadians(body.Lambda);
            double lambda0 = Angle.ToRadians(sun.Lambda);

            double s = sun.Lambda;
            double b = body.Lambda;

            if (Math.Abs(s - b) > 180)
            {
                if (s < b)
                {
                    s += 360;
                }
                else
                {
                    b += 360;
                }
            }

            return Math.Sign(b - s) * Angle.ToDegrees(Math.Acos(Math.Cos(beta) * Math.Cos(lambda - lambda0)));
        }

        /// <summary>
        /// Calculates phase angle of celestial body
        /// </summary>
        /// <param name="psi">Geocentric elongation of the body.</param>
        /// <param name="R">Distance Earth-Sun, in any units</param>
        /// <param name="Delta">Distance Earth-body, in the same units</param>
        /// <returns>Phase angle, in degrees, from 0 to 180</returns>
        /// <remarks>
        /// AA(II), formula 48.3.
        /// </remarks>
        /// TODO: tests
        public static double PhaseAngle(double psi, double R, double Delta)
        {
            psi = Angle.ToRadians(Math.Abs(psi));
            double phaseAngle = Angle.ToDegrees(Math.Atan(R * Math.Sin(psi) / (Delta - R * Math.Cos(psi))));
            if (phaseAngle < 0) phaseAngle += 180;
            return phaseAngle;
        }

        /// <summary>
        /// Gets phase value (illuminated fraction of the disk).
        /// </summary>
        /// <param name="phaseAngle">Phase angle of celestial body, in degrees.</param>
        /// <returns>Illuminated fraction of the disk, from 0 to 1.</returns>
        /// <remarks>
        /// AA(II), formula 48.1
        /// </remarks>
        // TODO: tests
        public static double Phase(double phaseAngle)
        {
            return (1 + Math.Cos(Angle.ToRadians(phaseAngle))) / 2;
        }


        // TODO: not finished yet
        public static RTS RiseTransitSet(double jd, CrdsEquatorial[] eq, CrdsGeographical location, double siderealTime, double h0)
        {
            if (eq.Length != 3)
                throw new ArgumentException("Number of equatorial coordinates in the array should be equal to 3.");

            double[] alpha = new double[3];
            double[] delta = new double[3];
            for (int i = 0; i < 3; i++)
            {
                alpha[i] = eq[i].Alpha;
                delta[i] = eq[i].Delta;
            }

            double cosH0 = (Math.Sin(Angle.ToRadians(h0)) - Math.Sin(Angle.ToRadians(location.Latitude)) * Math.Sin(Angle.ToRadians(delta[1]))) /
                (Math.Cos(Angle.ToRadians(location.Latitude)) * Math.Cos(Angle.ToRadians(delta[1])));

            if (Math.Abs(cosH0) >= 1)
            {
                throw new Exception("Circumpolar");
            }

            double H0 = Angle.ToDegrees(Math.Acos(cosH0));

            double[] m = new double[3];

            m[0] = (alpha[1] + location.Longitude - siderealTime) / 360;
            m[1] = m[0] - H0 / 360;
            m[2] = m[0] + H0 / 360;

            double deltaT = Date.DeltaT(jd);

            Angle.NormalizeAngles(alpha);
            Angle.NormalizeAngles(delta);

            double[] x = new double[] { 0, 0.5, 1 };

            for (int i = 0; i < 3; i++)
            {
                double th0 = siderealTime + 360.985647 * m[i];
                double n = m[i] + deltaT / 86400;

                double a = Angle.To360(Interpolation.Lagrange(x, alpha, n));
                double d = Interpolation.Lagrange(x, delta, n);

                var eq0 = new CrdsEquatorial(a, d);

                double H = Coordinates.HourAngle(th0, location.Longitude, a);
                var h = eq0.ToHorizontal(location, th0);

                double deltaM;

                // transit
                if (i == 0)
                {
                    deltaM = -H / 360;
                }
                else
                {
                    deltaM = (h.Altitude - h0) / (360 * Math.Cos(Angle.ToRadians(d)) * Math.Cos(Angle.ToRadians(location.Latitude) * Math.Sin(Angle.ToRadians(H))));
                }


            }

            return new RTS()
            {
                Rise = m[1] * 24,
                Transit = m[0] * 24,
                Set = m[2] * 24
            };
        }
    }
}
