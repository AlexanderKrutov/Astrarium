﻿namespace Astrarium.Algorithms
{
    /// <summary>
    /// Contains quantities concerning the appearance of planet.
    /// </summary>
    public class PlanetAppearance
    {
        /// <summary>
        /// Position angle of planet rotation axis.
        /// Measured counter-clockwise from direction to celestial north pole towards planet north pole.
        /// </summary>
        public double P { get; set; }

        /// <summary>
        /// Planetocentric declination of the Earth.
        /// If positive, the planet northern pole is tilted towards the Earth.
        /// Measured in degrees.
        /// </summary>
        public double D { get; set; }

        /// <summary>
        /// Planetographic longitude of the central meridian.
        /// </summary>
        public double CM { get; set; }
    }
}
