using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Headless
{
    internal class Ball
    {
        public Vector2 previousPosition;
        public Vector2 position;
        
        public readonly float radius;

        public Ball(Vector2 initPos, float radius)
        {
            this.position = initPos;
            this.radius = radius;
        }

        public Ball(float radius) 
        {
            this.position = Vector2.Zero;
            this.radius = radius;
        }

        public bool HasHitWall(Wall wall, bool actOnBothSides)
        {
            var toWallOriginPrevious = wall.origin - previousPosition;
            var distanceToWallPrevious = Helpers.Dot(toWallOriginPrevious, wall.orthogonalVector) - radius;
            var toWallOrigin = wall.origin - position;
            var distanceToWall = Helpers.Dot(toWallOrigin, wall.orthogonalVector) - radius;

            var inlineDistancePrevious = Helpers.Dot(toWallOriginPrevious, wall.inlineVector);
            var inlineDistance = Helpers.Dot(toWallOrigin, wall.inlineVector);

            //Console.WriteLine("To Wall Dist: " + distanceToWall);
            //Console.WriteLine("Inline Dist: " + inlineDistance);

            if (distanceToWall <= 0 && distanceToWallPrevious > 0 && Mathf.Abs(inlineDistance) < wall.length / 2)
            {
                return true;
            }
            if (actOnBothSides)
            {
                //var distanceToWallPreviousOtherSide = Helpers.Dot(toWallOriginPrevious, wall.reverseOrthogonalVector) - radius;
                //var distanceToWallOtherSide = Helpers.Dot(toWallOrigin, wall.reverseOrthogonalVector) - radius;
                //Console.WriteLine("To wall other side dist: " + distanceToWallOtherSide);
                if (distanceToWall >= 0 && distanceToWallPrevious < 0 && Mathf.Abs(inlineDistance) < wall.length / 2)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasHitWall(Wall wall)
        {
            return HasHitWall(wall, false);
        }

        public bool HasHitAnyWall(Wall[] walls, bool actOnBothSides)
        {
            foreach (var wall in walls)
            {
                if (HasHitWall(wall, actOnBothSides))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasHitAnyWall(Wall[] walls)
        {
            return HasHitAnyWall(walls, false);
        }
    }
}
