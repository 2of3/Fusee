

namespace Fusee.Tests.Math.Core
{
    class CoreTest
    {
        public CoreTest()
        {
            this.DoMathTests();
            
        }
        public void DoMathTests()
        {
            var mt = new MathTests();

            mt.Add();
            mt.PointInTriSimpleTest();
            mt.PointInTriMassTest();
        }


    }
}
