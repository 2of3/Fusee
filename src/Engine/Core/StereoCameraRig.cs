using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusee.Math;

namespace Fusee.Engine
{
    public class StereoCameraRig : Stereo3D
    {
        public StereoCameraRig(Stereo3DMode mode, int width, int height, float IOD = 0.065f) : base(mode, width, height)
        {
            Stereo3DParams.EyeDistance = IOD;
        }

        //LookAtS3D -Shift -- override/hide
        public override float4x4 LookAt3D(Stereo3DEye eye, float3 eyeV, float3 target, float3 up)
        {
            //var x = (eye == Stereo3DEye.Left)
            //    ? eyeV.x - Stereo3DParams.EyeDistance
            //    : eyeV.x + Stereo3DParams.EyeDistance;

            //var newEye = new float3(x, eyeV.y, eyeV.z);
            //var newTarget = new float3(target.x, target.y, target.z);

            //// Lookat with frustum shift
            //return float4x4.LookAt(newEye, newTarget, up);
            var retval = base.LookAt3D( eye, eyeV, target, up);

         //   double4x4 frustum = double4x4.CreatePerspectiveOffCenter();


            return retval;

        }
    }
}
