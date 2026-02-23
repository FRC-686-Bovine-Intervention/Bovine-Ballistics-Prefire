using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Headless
{
    internal class Wall
    {
        public readonly Vector2 leftPoint;
        public readonly Vector2 rightPoint;
        public readonly Vector2 origin;

        public readonly Vector2 orthogonalVector;
        public readonly Vector2 reverseOrthogonalVector;
        public readonly Vector2 inlineVector;

        public readonly float length;

        public Wall(Vector2 leftPoint, Vector2 rightPoint)
        {
            this.leftPoint = leftPoint;
            this.rightPoint = rightPoint;

            this.origin = (leftPoint + rightPoint) / 2;

            this.inlineVector = Helpers.normalize(leftPoint - rightPoint);
            this.orthogonalVector = new Vector2(-inlineVector.Y, inlineVector.X);
            this.reverseOrthogonalVector = -orthogonalVector;

            this.length = Helpers.norm(rightPoint - leftPoint);
        }
        public Wall(Vector2 origin, float length, float angleDegrees)
        {
            this.origin = origin;
            var leftNorm = new Vector2(Mathf.Cos(angleDegrees * Helpers.deg2rad), Mathf.Sin(angleDegrees * Helpers.deg2rad));
            var rightNorm = -leftNorm;
            this.leftPoint = origin + leftNorm * length / 2;
            this.rightPoint = origin + rightNorm * length / 2;

            this.inlineVector = Helpers.normalize(leftPoint - rightPoint);
            this.orthogonalVector = new Vector2(-inlineVector.Y, inlineVector.X);
            this.reverseOrthogonalVector = -orthogonalVector;

            this.length = length;
        }

        //CONSTANTS
        public static Wall floor = new Wall(new Vector2(-200, 0), new Vector2(200, 0));
        public static Vector2 hubOrigin = new Vector2(0, 1.8288f);
        public static Wall[] hub = CreateRectangle(1.688288f, 1.383801f, new Vector2(0, -1.1368f) + hubOrigin);
        public static Wall hubTop = new Wall(new Vector2(1.06f/2, 0) + hubOrigin, new Vector2(-1.06f, 0) + hubOrigin);
        public static Wall hubBottom = new Wall(new Vector2(0.605f/2, -0.39f) + hubOrigin, new Vector2(-0.605f, -0.39f) + hubOrigin);
        public static Wall hub1 = new Wall(new Vector2(0.415f, -0.2f) + hubOrigin, 0.47f, -30f);
        public static Wall hub2 = new Wall(new Vector2(-0.415f, -0.2f) + hubOrigin, 0.47f, 30f);

        public static Wall[] allKillWalls = new Wall[]
        {
            floor,
            hub[0], hub[1], hub[2], hub[3],
            hub1, hub2
        };
        
        public static Wall[] CreateRectangle(float width, float height, Vector2 center)
        {
            var walls = new Wall[4];
            walls[0] = new Wall(center + new Vector2(-width / 2, +height / 2), center + new Vector2(-width / 2, -height / 2));
            walls[1] = new Wall(center + new Vector2(+width / 2, -height / 2), center + new Vector2(+width / 2, +height / 2));
            walls[2] = new Wall(center + new Vector2(+width / 2, +height / 2), center + new Vector2(-width / 2, +height / 2));
            walls[3] = new Wall(center + new Vector2(-width / 2, -height / 2), center + new Vector2(+width / 2, -height / 2));
            return walls;
        }
    }
}
