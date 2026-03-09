using nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Headless
{
    internal class Helpers
    {
        public static float deg2rad = (float)(Math.PI / 180);
        public static float norm(Vector2 v) => (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
        public static Vector2 normalize(Vector2 v) => v / norm(v);

        public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
    }

    internal class Mathf
    {
        public static float PI = (float)Math.PI;
        public static float Sqrt(float x) => (float)Math.Sqrt(x);
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);
        public static float Abs(float x) => (float)Math.Abs(x);
        public static float Cos(float x) => (float)Math.Cos(x);
        public static float Sin(float x) => (float)Math.Sin(x);
        public static float Tan(float x) => (float)Math.Tan(x);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
    }
}
