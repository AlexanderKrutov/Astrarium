﻿using Astrarium.Algorithms;
using Astrarium.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Astrarium.Plugins.Tracks
{
    /// <summary>
    /// Renders celestial bodies motion tracks on the map
    /// </summary>
    public class TrackRenderer : BaseRenderer
    {
        private readonly TrackCalc trackCalc;
        private readonly Font fontLabel = new Font("Arial", 8);

        public TrackRenderer(TrackCalc trackCalc)
        {
            this.trackCalc = trackCalc;
        }

        public override void Render(IMapContext map)
        {
            var tracks = trackCalc.Tracks;

            foreach (var track in tracks)
            {
                if (track.Points.Any())
                {
                    var penTrack = new Pen(new SolidBrush(track.Color.GetColor(map.Schema, map.DayLightFactor)));

                    var segments = track.Points
                        .Select(p => Angle.Separation(p.Horizontal, map.Center) < map.ViewAngle ? p : null)
                        .Split(p => p == null, true);

                    foreach (var segment in segments)
                    {
                        var prevP = track.Points.Prev(segment.First());
                        if (prevP != null)
                        {
                            segment.Insert(0, prevP);
                        }

                        var nextP = track.Points.Next(segment.Last());
                        if (nextP != null)
                        {
                            segment.Add(nextP);
                        }

                        PointF pBody = IsSegmentContainsBody(map, segment, track) ? map.Project(track.Body.Horizontal) : PointF.Empty;
                        DrawTrackSegment(map, penTrack, segment.Select(p => map.Project(p.Horizontal)).ToArray(), pBody);
                    }

                    if (!segments.Any())
                    {
                        var segment = new List<CelestialPoint>();
                        var p0 = track.Points.OrderBy(p => Angle.Separation(p.Horizontal, map.Center)).First();
                        segment.Add(p0);

                        var p1 = track.Points.Prev(p0);
                        var p2 = track.Points.Next(p0);

                        if (p1 != null)
                        {
                            segment.Insert(0, p1);
                        }

                        if (p2 != null)
                        {
                            segment.Add(p2);
                        }

                        PointF pBody = IsSegmentContainsBody(map, segment, track) ? map.Project(track.Body.Horizontal) : PointF.Empty;
                        DrawTrackSegment(map, penTrack, segment.Select(p => map.Project(p.Horizontal)).ToArray(), pBody);
                    }

                    if (track.DrawLabels)
                    {
                        DrawLabels(map, track);
                    }
                }
            }
        }

        public override RendererOrder Order => RendererOrder.SolarSystem - 1;

        private bool IsSegmentContainsBody(IMapContext map, ICollection<CelestialPoint> segment, Track track)
        {
            int firstIndex = track.Points.IndexOf(segment.First());
            int lastIndex = track.Points.IndexOf(segment.Last());
            double from = (double)firstIndex / (track.Points.Count - 1) * track.Duration + track.From;
            double to = (double)lastIndex / (track.Points.Count - 1) * track.Duration + track.From;
            return map.JulianDay > from && map.JulianDay < to;            
        }

        private void DrawLabels(IMapContext map, Track track)
        {
            var brushLabel = new SolidBrush(track.Color.GetColor(map.Schema, map.DayLightFactor));
            double trackStep = track.Step;
            double stepLabels = track.LabelsStep.TotalDays;

            int each = (int)(stepLabels / trackStep);

            double jd = track.From;
            for (int i = 0; i < track.Points.Count; i++)
            {
                if (i % each == 0 || i == track.Points.Count - 1)
                {
                    var tp = track.Points[i];
                    double ad = Angle.Separation(tp.Horizontal, map.Center);
                    if (ad < map.ViewAngle)
                    {
                        PointF p = map.Project(tp.Horizontal);
                        if (!map.IsOutOfScreen(p))
                        {
                            map.Graphics.FillEllipse(brushLabel, p.X - 2, p.Y - 2, 4, 4);
                            map.DrawObjectCaption(fontLabel, brushLabel, Formatters.DateTime.Format(new Date(jd, map.GeoLocation.UtcOffset)), p, 4);
                        }
                    }
                }

                jd += trackStep;
            }
        }

        private void DrawTrackSegment(IMapContext map, Pen penGrid, PointF[] points, PointF pBody, int iterationStep = 0)
        {
            iterationStep++;

            // Do not draw figure containing less than 2 points
            if (points.Length < 2)
            {
                return;
            }
            // Two points can be simply drawn as a line
            else if (points.Length == 2)
            {
                points = ShiftToAncorPoint(map, points, pBody);
                map.Graphics.DrawLine(penGrid, points[0], points[1]);
            }
            // interpolation is needed
            else if (points.Length > 2 && points.Length < 20 && iterationStep < 10)
            {
                // Coordinates of the screen center
                var origin = new PointF(map.Width / 2, map.Height / 2);

                // Screen diagonal
                double diag = Math.Sqrt(map.Width * map.Width + map.Height * map.Height);

                float maxX = points.Select(p => Math.Abs(p.X)).Max();
                float maxY = points.Select(p => Math.Abs(p.Y)).Max();

                float f = (float)diag / Math.Max(maxX, maxY);

                var scaledPoints = points.Select(p => new PointF(p.X * f, p.Y * f)).ToArray();

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddCurve(scaledPoints);
                    gp.Flatten();
                    scaledPoints = gp.PathPoints.Select(p => new PointF(p.X / f, p.Y / f)).ToArray();

                    var segments = scaledPoints.Select(p => map.DistanceBetweenPoints(p, origin) < diag * 3 ? p : PointF.Empty)
                        .Split(p => p == PointF.Empty, true);

                    foreach (var segment in segments)
                    {
                        var p1 = scaledPoints.Prev(segment.First());
                        var p2 = scaledPoints.Next(segment.Last());

                        List<PointF> newPoints = new List<PointF>(segment);

                        if (p1 != PointF.Empty)
                        {
                            newPoints.Insert(0, p1);
                        }

                        if (p2 != PointF.Empty)
                        {
                            newPoints.Add(p2);
                        }

                        DrawTrackSegment(map, penGrid, newPoints.ToArray(), pBody, iterationStep);
                    }

                    if (!segments.Any())
                    {
                        var p0 = scaledPoints.OrderBy(p => map.DistanceBetweenPoints(p, origin)).First();
                        var p1 = scaledPoints.Prev(p0);
                        var p2 = scaledPoints.Next(p0);

                        List<PointF> newPoints = new List<PointF>();
                        if (p1 != PointF.Empty)
                        {
                            newPoints.Insert(0, p1);
                        }
                        if (p2 != PointF.Empty)
                        {
                            newPoints.Add(p2);
                        }

                        DrawTrackSegment(map, penGrid, newPoints.ToArray(), pBody, iterationStep);
                    }
                }
            }
            // draw the curve in regular way
            else
            {
                map.Graphics.DrawCurve(penGrid, ShiftToAncorPoint(map, points, pBody));
            }
        }

        /// <summary>
        /// Shifts all points of the curve (or line) to the ancor point.
        /// </summary>
        /// <param name="points">Points of the curve (or line)</param>
        /// <param name="p0">Ancor point. All curve or line points will be corrected, 
        /// so the shifted curve (or line) will intersect the ancor point.</param>
        /// <returns>Corrected points of the curve (or line)</returns>
        private PointF[] ShiftToAncorPoint(IMapContext map, PointF[] points, PointF p0)
        {
            if (p0 != PointF.Empty)
            {
                PointF proj = GetProjectedPoint(map, points, p0);

                if (map.DistanceBetweenPoints(proj, p0) > 1)
                {
                    float dx = p0.X - proj.X;
                    float dy = p0.Y - proj.Y;
                    
                    points = points.Select(p => new PointF(p.X + dx, p.Y + dy)).ToArray();
                }
            }

            return points;
        }

        /// <summary>
        /// Gets nearest point on the curve (or the line) to the provided point.
        /// </summary>
        /// <param name="points">Points of the curve (or the line)</param>
        /// <param name="p0">Some point to find the nearest one on the curve (on the line), i.e. projection.</param>
        /// <returns>Nearest point on the curve (or the line), i.e. projection of the point p0.</returns>
        private PointF GetProjectedPoint(IMapContext map, PointF[] points, PointF p0)
        {
            PointF[] nearest = points.OrderBy(n => map.DistanceBetweenPoints(n, p0)).Take(2).ToArray();

            PointF p1 = nearest[0];
            PointF p2 = nearest[1];

            PointF e1 = new PointF(p2.X - p1.X, p2.Y - p1.Y);
            PointF e2 = new PointF(p0.X - p1.X, p0.Y - p1.Y);
            double val = e1.X * e2.X + e1.Y * e2.Y;
            double len = e1.X * e1.X + e1.Y * e1.Y;
            PointF p = new PointF((float)(p1.X + val * e1.X / len),
                                  (float)(p1.Y + val * e1.Y / len));
            return p;
        }
    }
}
