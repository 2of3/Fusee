using System;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using NUnit.Framework;
using Fusee.Math;
using NUnit.Framework.Internal;

namespace Fusee.Tests.Math.Core
{
    [TestFixture]
    class MathTests
    {
        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void Add()
        {
            double2 V1, V2, Res;
            V1 = new double2(5.5,5.5);
            V2 = new double2(1.5, 2.5);
            Res = double2.Add(V1, V2);

            Assert.AreEqual(7, Res.x);
            Assert.AreEqual(8, Res.y);

            Res = double2.Add(V2, V1);

            Assert.AreEqual(7, Res.x);
            Assert.AreEqual(8, Res.y);
        }

        [Test]
        public void PointInTriSimpleTest()
        {
            float2 a = new float2(-1, -1), b = new float2(0, 1), c = new float2(1, -1);
            float2 pickPoint = new float2(0, 0);
            float wa, wb, wc;
            bool ret;

            // <Point is inside>
            ret = MathHelper.IsPointInTriCW(pickPoint, b, c, a, out wa, out wb, out wc);
            ret = MathHelper.IsPointInTriCW(pickPoint, c, a, b, out wa, out wb, out wc);
            ret = MathHelper.IsPointInTriCW(pickPoint, a, b, c, out wa, out wb, out wc);
            Assert.IsTrue(ret);
            Assert.AreEqual(wa, 0.25);
            Assert.AreEqual(wb, 0.5);
            Assert.AreEqual(wc, 0.25);

            ret = MathHelper.IsPointInTriCCW(pickPoint, a, b, c, out wa, out wb, out wc);
            Assert.IsFalse(ret);

            ret = MathHelper.IsPointInTriCCW(pickPoint, b, a, c, out wa, out wb, out wc);
            Assert.IsTrue(ret);
            Assert.AreEqual(wa, 0.25);
            Assert.AreEqual(wb, 0.5);
            Assert.AreEqual(wc, 0.25);

            ret = MathHelper.IsPointInTri(pickPoint, a, b, c, out wa, out wb, out wc);
            Assert.IsTrue(ret);
            Assert.AreEqual(wa, 0.25);
            Assert.AreEqual(wb, 0.5);
            Assert.AreEqual(wc, 0.25);

            // <Point is outside>
            pickPoint = new float2(1, 1);
            ret = MathHelper.IsPointInTriCW(pickPoint, a, b, c, out wa, out wb, out wc);
            Assert.IsFalse(ret);

            ret = MathHelper.IsPointInTriCCW(pickPoint, a, b, c, out wa, out wb, out wc);
            Assert.IsFalse(ret);

            ret = MathHelper.IsPointInTri(pickPoint, a, b, c, out wa, out wb, out wc);
            Assert.IsFalse(ret);
        }


        [Test]
        public void PointInTriMassTest()
        {
            const int n = 100000;
            Random rnd = new Random();
            float2[] verts = new float2[n*3];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = new float2((float)rnd.NextDouble() * 2000 - 1000, (float)rnd.NextDouble() * 2000 - 1000);
            }

            bool retCW, retCCW, ret;
            float wa, wb, wc;

            float2 pickPoint = new float2((float)rnd.NextDouble() * 2000 - 1000, (float)rnd.NextDouble() * 2000 - 1000);
            for (int i = 0; i < n; i++)
            {
                retCW = MathHelper.IsPointInTriCW(pickPoint, verts[i], verts[i+1], verts[i+2], out wa, out wb, out wc);
                retCCW = MathHelper.IsPointInTriCCW(pickPoint, verts[i], verts[i+1], verts[i+2], out wa, out wb, out wc);
                ret = MathHelper.IsPointInTri(pickPoint, verts[i], verts[i+1], verts[i+2], out wa, out wb, out wc);

                // Either the point is inside - then one of clockwise or counterclockwise must hold. 
                // Or the pickpoint is entirely outside - then neither CW nor CCW holds.
                if (ret)
                    Assert.IsTrue(retCW == !retCCW);
                else
                    Assert.IsTrue(!retCW && !retCCW);
            }
        }

        [Test]
        public void PointInTriIndividualTest()
        {
            float2 a = new float2(181.3256f, 486.9698f), b = new float2(295.1436f, 742.6833f), c = new float2(-204.2623f, -146.3234f);
            float2 pickPoint = new float2(162.0107f, 388.1996f);
            float wa, wb, wc;
            bool ret, retCW, retCCW;

            // <Point is inside>
            retCW = MathHelper.IsPointInTriCW(pickPoint, a, b, c, out wa, out wb, out wc);
            retCCW = MathHelper.IsPointInTriCCW(pickPoint, a, b, c, out wa, out wb, out wc);
            ret = MathHelper.IsPointInTri(pickPoint, a, b, c, out wa, out wb, out wc);

            if (ret)
                Assert.IsTrue(retCW == !retCCW);
            else
                Assert.IsTrue(!retCW && !retCCW);
        }
    }
}
