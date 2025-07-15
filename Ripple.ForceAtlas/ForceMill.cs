using System.Runtime.InteropServices.JavaScript;

namespace Ripple.ForceAtlas;

using System.Collections.Generic;

/// <summary>
///     Generates the forces on demand, here are all the formulas for attraction and repulsion.
/// </summary>
public static class ForceMill
{
    public static RepulsionForce BuildRepulsion(bool isAdjustBySize, double coefficient, double edgeGamma) =>
        isAdjustBySize ? new LinearRepulsionAntiCollision(coefficient, edgeGamma) : new LinearRepulsion(coefficient, edgeGamma);

    public static RepulsionForce GetStrongGravity(double coefficient) => new StrongGravity(coefficient);

    public static AttractionForce BuildAttraction(bool isLogAttraction, bool isDistributedAttraction, bool isAdjustBySize, double coefficient) =>
        isAdjustBySize
            ? isLogAttraction
                ? isDistributedAttraction
                    ? new LogAttractionDegreeDistributedAntiCollision(coefficient)
                    : new LogAttractionAntiCollision(coefficient)
                : isDistributedAttraction
                    ? new LinearAttractionDegreeDistributedAntiCollision(coefficient)
                    : new LinearAttractionAntiCollision(coefficient)
            : isLogAttraction
                ? isDistributedAttraction
                    ? new LogAttractionDegreeDistributed(coefficient)
                    : new LogAttraction(coefficient)
                : isDistributedAttraction
                    ? new LinearAttractionMassDistributed(coefficient)
                    : new LinearAttraction(coefficient);

    public abstract class AttractionForce
    {
        public abstract void Apply(IParticle n1, IParticle n2,
            double e); // Model for node-node attraction (e is for edge weight if needed)
    }

    public abstract class RepulsionForce
    {
        public abstract void Apply(IParticle n1, Edge edge); // Model for node-edge repulsion
        public abstract void Apply(IParticle n1, IParticle n2); // Model for node-node repulsion

        public abstract void Apply(IParticle n, Region r); // Model for Barnes Hut approximation

        public abstract void Apply(IParticle n, double g); // Model for gravitation (anti-repulsion)
    }

    private class LinearRepulsion(double c, double edgeGamma) : RepulsionForce
    {
        public override void Apply(IParticle n1, Edge edge)
        {
            if (edge.Source == n1 || edge.Target == n1)
                return;

            var dot = (n1.X - edge.Source.X) * edge.Nx + (n1.Y - edge.Source.Y) * edge.Ny;
            var px = edge.Nx * dot + edge.Source.X;
            var py = edge.Ny * dot + edge.Source.Y;   
            var nn = (Node)n1;
            var restrictions = nn.Restrictions;
            var sourceRestrictions = edge.Source.Restrictions;
            var targetRestrictions = edge.Target.Restrictions;

            // is p between the two ends of the edge?
            if (px < edge.Source.X && px < edge.Target.X || px > edge.Source.X && px > edge.Target.X)
            {
                var sourceXDist = edge.Source.X - n1.X;
                var sourceYDist = edge.Source.Y - n1.Y;
                var targetXDist = edge.Target.X - n1.X;
                var targetYDist = edge.Target.Y - n1.Y;
                var sourceDistance = Math.Sqrt(sourceXDist * sourceXDist + sourceYDist * sourceYDist);
                var targetDistance = Math.Sqrt(targetXDist * targetXDist + targetYDist * targetYDist);
                
                for (var i = 0; i < 8; i++)
                {
                    restrictions[i] = Math.Min(nn.Restrictions[i], Math.Min(sourceDistance, targetDistance) / 3);
                    sourceRestrictions[i] = Math.Min(edge.Source.Restrictions[i], sourceDistance / 3);
                    targetRestrictions[i] = Math.Min(edge.Target.Restrictions[i], targetDistance / 3);
                }
                nn.Restrictions = restrictions;
                edge.Source.Restrictions = sourceRestrictions;
                edge.Target.Restrictions = targetRestrictions;

                return;
            }

            // Get the distance
            var xDist = px - n1.X;
            var yDist = py - n1.Y;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            int Mod(int a, int b) => (a % b + b) % b;

            // restrict movement
            var arc = (int)Math.Floor(Math.Atan2(-yDist, xDist) * 4 / Math.PI);
            for (var i = 0; i < 5; i++)
            {
                var j = Mod(arc + (i - 2), 8);
                restrictions[j] = Math.Min(nn.Restrictions[j], distance / 3);
                j = Mod(arc + i + 2, 8);
                sourceRestrictions[j] = Math.Min(edge.Source.Restrictions[j], distance / 3);
                targetRestrictions[j] = Math.Min(edge.Target.Restrictions[j], distance / 3);
            }
            nn.Restrictions = restrictions;
            edge.Source.Restrictions = sourceRestrictions;
            edge.Target.Restrictions = targetRestrictions;

            if (distance > edgeGamma)
                return;

            // NB: factor = force / distance
            var offset = edgeGamma - distance;
            var factor = 1 * offset * offset / distance;

            n1.Dx -= xDist * factor;
            n1.Dy -= yDist * factor;
        }
        public override void Apply(IParticle n1, IParticle n2)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = c + 10 * n1.Mass * n2.Mass / distance / distance;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }

        public override void Apply(IParticle n, Region r)
        {
            // Get the distance
            var xDist = n.X - r.MassCenterX;
            var yDist = n.Y - r.MassCenterY;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = c * n.Mass * r.Mass / distance / distance;

            n.Dx += xDist * factor;
            n.Dy += yDist * factor;
        }

        public override void Apply(IParticle n, double g)
        {
            // Get the distance
            var xDist = n.X;
            var yDist = n.Y;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = c * n.Mass * g / distance;

            n.Dx -= xDist * factor;
            n.Dy -= yDist * factor;
        }
    }

    /// <summary>
    /// Repulsion force: Strong Gravity (as a Repulsion Force because it is easier)
    /// </summary>
    /// <param name="c"></param>
    private class LinearRepulsionAntiCollision(double c, double edgeGamma) : RepulsionForce
    {
        public override void Apply(IParticle n1, Edge edge)
        {
            if (edge.Source == n1 || edge.Target == n1)
                return;
            var vx = edge.Target.X - edge.Source.X;
            var vy = edge.Target.Y - edge.Source.Y;
            var magnitude = Math.Sqrt(vx * vx + vy * vy);
            var nx = vx / magnitude;
            var ny = vy / magnitude;
            var dot = n1.X * nx + n1.Y * ny;
            var px = nx * dot;
            var py = ny * dot;
            var nn = (Node)n1;
            var restrictions = nn.Restrictions;
            var sourceRestrictions = edge.Source.Restrictions;
            var targetRestrictions = edge.Target.Restrictions;

            // is p between the two ends of the edge?
            if (px < edge.Source.X && px < edge.Target.X || px > edge.Source.X && px > edge.Target.X)
            {
                var sourceXDist = edge.Source.X - n1.X;
                var sourceYDist = edge.Source.Y - n1.Y;
                var targetXDist = edge.Target.X - n1.X;
                var targetYDist = edge.Target.Y - n1.Y;
                var sourceDistance = Math.Sqrt(sourceXDist * sourceXDist + sourceYDist * sourceYDist);
                var targetDistance = Math.Sqrt(targetXDist * targetXDist + targetYDist * targetYDist);

                for (var i = 0; i < 8; i++)
                {
                    restrictions[i] = Math.Min(nn.Restrictions[i], Math.Min(sourceDistance, targetDistance) / 3);
                    sourceRestrictions[i] = Math.Min(edge.Source.Restrictions[i], sourceDistance / 3);
                    targetRestrictions[i] = Math.Min(edge.Target.Restrictions[i], targetDistance / 3);
                }
                nn.Restrictions = restrictions;
                edge.Source.Restrictions = sourceRestrictions;
                edge.Target.Restrictions = targetRestrictions;

                return;
            }

            // Get the distance
            var xDist = n1.X - px;
            var yDist = n1.Y - py;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            // restrict movement
            var arc = (int)Math.Floor(Math.Atan2(xDist, yDist) * 4 / Math.PI);
            for (var i = 0; i < 5; i++)
            {
                var j = (arc + (i - 2)) % 8;
                restrictions[j] = Math.Min(nn.Restrictions[j], distance / 3);
                j = (arc + i + 2) % 8;
                sourceRestrictions[j] = Math.Min(edge.Source.Restrictions[j], distance / 3);
                targetRestrictions[j] = Math.Min(edge.Target.Restrictions[j], distance / 3);
            }
            nn.Restrictions = restrictions;
            edge.Source.Restrictions = sourceRestrictions;
            edge.Target.Restrictions = targetRestrictions;

            if (distance > edgeGamma)
                return;
            // NB: factor = force / distance
            var offset = edgeGamma - distance;

            switch (distance)
            {
                case > 0:
                    {
                        // NB: factor = force / distance
                        var factor = c * offset * offset / distance;

                        n1.Dx += xDist * factor;
                        n1.Dy += yDist * factor;
                        break;
                    }
                case < 0:
                    {
                        var factor = 100 * c * n1.Mass;

                        n1.Dx += xDist * factor;
                        n1.Dy += yDist * factor;
                        break;
                    }
            }
        }
        public override void Apply(IParticle n1, IParticle n2)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            var distance = Math.Sqrt(xDist * xDist + yDist * yDist) - n1.Size - n2.Size;

            switch (distance)
            {
                case > 0:
                    {
                        // NB: factor = force / distance
                        var factor = c * n1.Mass * n2.Mass / distance / distance;

                        n1.Dx += xDist * factor;
                        n1.Dy += yDist * factor;

                        n2.Dx -= xDist * factor;
                        n2.Dy -= yDist * factor;
                        break;
                    }
                case < 0:
                    {
                        var factor = 100 * c * n1.Mass * n2.Mass;

                        n1.Dx += xDist * factor;
                        n1.Dy += yDist * factor;

                        n2.Dx -= xDist * factor;
                        n2.Dy -= yDist * factor;
                        break;
                    }
            }
        }

        public override void Apply(IParticle n, Region r)
        {
            // Get the distance
            var xDist = n.X - r.MassCenterX;
            var yDist = n.Y - r.MassCenterY;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            switch (distance)
            {
                case > 0:
                    {
                        // NB: factor = force / distance
                        var factor = c * n.Mass * r.Mass / distance / distance;

                        n.Dx += xDist * factor;
                        n.Dy += yDist * factor;
                        break;
                    }
                case < 0:
                    {
                        var factor = -c * n.Mass * r.Mass / distance;

                        n.Dx += xDist * factor;
                        n.Dy += yDist * factor;
                        break;
                    }
            }
        }

        public override void Apply(IParticle n, double g)
        {
            // Get the distance
            var xDist = n.X;
            var yDist = n.Y;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = c * n.Mass * g / distance;

            n.Dx -= xDist * factor;
            n.Dy -= yDist * factor;
        }
    }

    private class StrongGravity(double c) : RepulsionForce
    {
        public override void Apply(IParticle n1, Edge edge) { /* Not Relevant */ }
        public override void Apply(IParticle n1, IParticle n2) { /* Not Relevant */ }

        public override void Apply(IParticle n, Region r) { /* Not Relevant */ }

        public override void Apply(IParticle n, double g)
        {
            // Get the distance
            var xDist = n.X;
            var yDist = n.Y;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = c * n.Mass * g;

            n.Dx -= xDist * factor;
            n.Dy -= yDist * factor;
        }
    }

    private class LinearAttraction(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            var x2 = xDist * xDist;
            var y2 = yDist * yDist;
            var distance = (float)Math.Sqrt(x2 + y2) - c;
            var ratio = x2 / (x2 + y2);

            // NB: factor = force / distance
            var springForce = e * distance;

            xDist *= springForce * ratio;
            yDist *= springForce * (1 - ratio);

            n1.Dx -= xDist;
            n1.Dy -= yDist;

            n2.Dx += xDist;
            n2.Dy += yDist;
        }
    }

    private class LinearAttractionORIG(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;

            // NB: factor = force / distance
            var factor = -c * e;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }

    /// <summary>
    /// Attraction force: Linear, distributed by mass (typically, degree)
    /// </summary>
    /// <param name="c"></param>
    private class LinearAttractionMassDistributed(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;

            // NB: factor = force / distance
            var factor = -c * e / n1.Mass;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }

    private class LogAttraction(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = -c * e * Math.Log(1 + distance) / distance;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }

    private class LogAttractionDegreeDistributed(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            double distance = (float)Math.Sqrt(xDist * xDist + yDist * yDist);

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = -c * e * Math.Log(1 + distance) / distance / n1.Mass;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }

    private class LinearAttractionAntiCollision(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            var distance = Math.Sqrt(xDist * xDist + yDist * yDist) - n1.Size - n2.Size;

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = -c * e;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }

    private class LinearAttractionDegreeDistributedAntiCollision(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            var distance = Math.Sqrt(xDist * xDist + yDist * yDist) - n1.Size - n2.Size;

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = -c * e / n1.Mass;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }

    private class LogAttractionAntiCollision(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            var distance = Math.Sqrt(xDist * xDist + yDist * yDist) - n1.Size - n2.Size;

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = -c * e * Math.Log(1 + distance) / distance;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }

    private class LogAttractionDegreeDistributedAntiCollision(double c) : AttractionForce
    {
        public override void Apply(IParticle n1, IParticle n2, double e)
        {
            // Get the distance
            var xDist = n1.X - n2.X;
            var yDist = n1.Y - n2.Y;
            var distance = Math.Sqrt(xDist * xDist + yDist * yDist) - n1.Size - n2.Size;

            if (distance <= 0)
                return;
            // NB: factor = force / distance
            var factor = -c * e * Math.Log(1 + distance) / distance / n1.Mass;

            n1.Dx += xDist * factor;
            n1.Dy += yDist * factor;

            n2.Dx -= xDist * factor;
            n2.Dy -= yDist * factor;
        }
    }
}