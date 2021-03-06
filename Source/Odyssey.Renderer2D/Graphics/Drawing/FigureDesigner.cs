﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Odyssey.UserInterface.Style;
using SharpDX.Mathematics;

namespace Odyssey.Graphics.Drawing
{
    public class FigureDesigner
    {
        private readonly List<VectorCommand> commands;

        public FigureDesigner()
        {
            commands = new List<VectorCommand>();
        }

        public void DrawRingSegment(float angleFrom, float angleTo, Vector2 center, float outerRadius, float innerRadius, bool isClosed = true)
        {
            float t0 = angleFrom;
            float t1 = angleTo;
            float r1 = outerRadius;
            float r2 = innerRadius;

            AddCommand(CommandType.Move, new[] {center.X + (float) Math.Sin(t0)*r2, center.Y - (float) Math.Cos(t0)*r2});
            AddCommand(CommandType.EllipticalArc, new[] {r2, r2, 0, 0, 1, center.X + (float) Math.Sin(t1)*r2, center.Y - (float) Math.Cos(t1)*r2});
            AddCommand(CommandType.Line, new[] {center.X + (float) Math.Sin(t1)*r1, center.Y - (float) Math.Cos(t1)*r1});
            AddCommand(CommandType.EllipticalArc, new[] {r1, r1, 0, 0, 0, center.X + (float) Math.Sin(t0)*r1, center.Y - (float) Math.Cos(t0)*r1});
            
            if (isClosed)
                AddCommand(CommandType.Close, null);

        }

        public void DrawRightTrapezoid(Vector3 topLeft, float baseTop, float baseBottom, float height)
        {
            AddCommand(CommandType.Move, new[] { topLeft.X, topLeft.Y });
            AddCommand(CommandType.Line, new[] { baseTop, 0 }, true);
            AddCommand(CommandType.Line, new[] { baseBottom-baseTop, height }, true);
            AddCommand(CommandType.Line, new[] { -baseBottom, 0 }, true);
            AddCommand(CommandType.Close, null);
        }

        public void AddCommand(VectorCommand command)
        {
            Contract.Requires<ArgumentNullException>(command!=null, "command");
            commands.Add(command);
        }

        public void AddCommand(CommandType type, float[] arguments, bool isRelative = false)
        {
            AddCommand(new VectorCommand(type, arguments, isRelative));
        }

        public IEnumerable<VectorCommand> Result { get { return commands; } }

        public string ResultString
        {
            get
            {
                if (commands.Count == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                foreach (var command in commands)
                {
                    char cmd = VectorArtParser.ConvertBack(command.Type, command.IsRelative);
                    sb.Append(cmd);
                    if (command.Arguments == null)
                        continue;
                    foreach (float f in command.Arguments)
                    {
                        sb.AppendFormat("{0} ", f);
                    }
                }
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
        }
    }
}
