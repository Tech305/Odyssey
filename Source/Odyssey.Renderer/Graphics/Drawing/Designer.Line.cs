﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Odyssey.Geometry;
using SharpDX.Mathematics;

namespace Odyssey.Graphics.Drawing
{
    public partial class Designer
    {
        private delegate void RenderRectangle(RectangleF rectangle);

        void CreatePolyLine(IEnumerable<Vector2> points, float lineWidth, RenderRectangle callback, PolygonDirection direction)
        {
            Contract.Requires<ArgumentNullException>(points!=null, "points");
            Vector2[] pointArray = points as Vector2[] ?? points.ToArray();
            if (pointArray.Length < 2)
                throw new InvalidOperationException("At least two points are required");

            for (int i = 0; i < pointArray.Length - 1; i++)
            {
                Vector2 p0xy = pointArray[i];
                Vector2 p1xy = pointArray[i + 1];
                Vector3 p0 = new Vector3(p0xy, 0);
                Vector3 p1 = new Vector3(p1xy, 0);
                float d = Vector3.Distance(p0, p1);

                float xDiff = p1.X - p0.X;
                float yDiff = p1.Y - p0.Y;
                float angle;

                if (MathHelper.IsCloseToZero(xDiff))
                    angle = 0;
                else
                    angle = (float) Math.Atan2(yDiff, xDiff) - MathHelper.PiOverTwo;

                Matrix previousTransform = Transform;
                switch (direction)
                {
                    case PolygonDirection.PositiveY:
                        Transform = Matrix.RotationYawPitchRoll(0, 0, angle)*Matrix.Translation(p0)*Transform;
                        break;
                    case PolygonDirection.NegativeZ:
                        Transform = Matrix.RotationYawPitchRoll(MathHelper.PiOverTwo, -angle, 0)*Matrix.Translation(p0)*Transform;
                        break;

                    case PolygonDirection.PositiveZ:
                        Transform = Matrix.RotationYawPitchRoll(MathHelper.PiOverTwo, MathHelper.Pi-angle, MathHelper.Pi) * Matrix.Translation(p0) * Transform;
                        break;

                    default:
                        throw new NotImplementedException("direction");
                }

                callback(new RectangleF(-lineWidth / 2, 0, lineWidth, d));
                Transform = previousTransform;
            }
        }

        public void DrawPolyline(IEnumerable<Vector2> points, float lineWidth, float strokeThickness, PolygonDirection direction = PolygonDirection.PositiveY)
        {
            RenderRectangle callback = FillRectangle;
            CreatePolyLine(points, lineWidth, callback, direction);
        }

        public void FillPolyline(IEnumerable<Vector2> points, float lineWidth, PolygonDirection direction = PolygonDirection.PositiveY)
        {
            RenderRectangle callback = FillRectangle;
            CreatePolyLine(points, lineWidth, callback, direction);
        }

        public void FillClosedPolyline(IEnumerable<Vector2> points, float lineWidth, PolygonDirection direction = PolygonDirection.PositiveY)
        {
            RenderRectangle callback = FillRectangle;
            var pointList = new List<Vector2>(points);
            pointList.Add(pointList[0]);
            CreatePolyLine(pointList, lineWidth, callback, direction);
        }
    }
}