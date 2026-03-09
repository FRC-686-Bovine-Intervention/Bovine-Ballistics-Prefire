using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using nkast.Aether.Physics2D.Common;
using System.Diagnostics;

namespace Headless
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Fuel fuel = new Fuel();
            //fuel.init();
            //fuel.Launch(new Vector2(-0.5f, -0.5f) + Wall.hubOrigin, new Vector2(10, 10));
            //for (int i = 0; i < 400; i++)
            //{
            //    fuel.Update(0.01f);
            //}
            new Runner().Main(args);
        }
    }
}
