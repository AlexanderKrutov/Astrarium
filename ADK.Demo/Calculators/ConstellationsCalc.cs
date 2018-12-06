﻿using ADK.Demo.Objects;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace ADK.Demo.Calculators
{
    public class ConstellationsCalc : BaseSkyCalc
    {
        /// <summary>
        /// Constellations
        /// </summary>
        private List<Constellation> Constellations = new List<Constellation>();

        /// <summary>
        /// Constellations borders coordinates
        /// </summary>
        private List<List<CelestialPoint>> ConstBorders = new List<List<CelestialPoint>>();

        public ConstellationsCalc(Sky sky) : base(sky) { }

        public override void Calculate()
        {
            var p = Precession.ElementsFK5(Date.EPOCH_J2000, Sky.JulianDay);

            foreach (var b in ConstBorders)
            {
                foreach (var bp in b)
                {
                    // Equatorial coordinates for the mean equinox and epoch of the target date
                    var eq = Precession.GetEquatorialCoordinates(bp.Equatorial0, p);

                    // Apparent horizontal coordinates
                    bp.Horizontal = eq.ToHorizontal(Sky.GeoLocation, Sky.SiderealTime);
                }
            }

            foreach (var c in Constellations)
            {
                // Equatorial coordinates for the mean equinox and epoch of the target date
                var eq = Precession.GetEquatorialCoordinates(c.Label.Equatorial0, p);

                // Apparent horizontal coordinates
                c.Label.Horizontal = eq.ToHorizontal(Sky.GeoLocation, Sky.SiderealTime);
            }
        }

        public override void Initialize()
        {
            LoadBordersData();
            LoadLabelsData();
            LoadConstNames();
        }

        /// <summary>
        /// Loads constellation borders data
        /// </summary>
        private void LoadBordersData()
        {
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data/Borders.dat");

            using (var sr = new BinaryReader(new FileStream(file, FileMode.Open)))
            {
                List<CelestialPoint> block = null;
                while (sr.BaseStream.Position != sr.BaseStream.Length)
                {
                    bool start = sr.ReadBoolean();
                    if (start)
                    {
                        block = new List<CelestialPoint>();
                        ConstBorders.Add(block);
                    }

                    block.Add(new CelestialPoint()
                    {
                        Equatorial0 = new CrdsEquatorial(sr.ReadDouble(), sr.ReadDouble())
                    });
                }
            }

            Sky.AddDataProvider("ConstBorders", () => ConstBorders);
        }

        /// <summary>
        /// Loads constellations labels data
        /// </summary>
        private void LoadLabelsData()
        {
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data/Conlabels.dat");
            using (var sr = new BinaryReader(new FileStream(file, FileMode.Open)))
            {
                while (sr.BaseStream.Position != sr.BaseStream.Length)
                {
                    string code = sr.ReadString();                
                    Constellations.Add(new Constellation()
                    {
                        Code = code,
                        Label = new CelestialPoint()
                        {
                            Equatorial0 = new CrdsEquatorial(sr.ReadSingle(), sr.ReadSingle())
                        }
                    });
                }
            }

            Sky.AddDataProvider("Constellations", () => Constellations);
        }

        /// <summary>
        /// Loads constellation labels data
        /// </summary>
        private void LoadConstNames()
        {
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data/Connames.dat");
            string line = "";
            using (var sr = new StreamReader(file, Encoding.Default))
            {               
                while (line != null && !sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    var chunks = line.Split(';');
                    string code = chunks[0].Trim().ToUpper();
                    var constellation = Constellations.First(c => c.Code == code);
                    constellation.Name = chunks[1].Trim();
                    constellation.Genitive = chunks[2].Trim();
                }
            }
        }
    }
}