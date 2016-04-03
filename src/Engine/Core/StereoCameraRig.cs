﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusee.Math;

namespace Fusee.Engine
{
    public class StereoCameraRig : Stereo3D
    {
        public float4x4 CurrentProjection
        {
            get { return _currentProjection; }
        }

        public float3 CurrentOffest { get; private set; }
        private float4x4 _currentProjection, _leftFrustum, _rightFrustum;
       // private RenderContext RC { get; set; }


        public StereoCameraRig( Stereo3DMode mode, int width, int height, float IOD = 0.065f) : base(mode, width, height)
        {
            Stereo3DParams.EyeDistance = IOD;
        }

        public override void Prepare(Stereo3DEye eye)
        {
            base.Prepare(eye);
            if (eye == Stereo3DEye.Left)
            {
                _currentProjection = _leftFrustum;
                CurrentOffest = new float3(Stereo3DParams.EyeDistance * -0.5f, 0, 0);
            }
            else
            {
                _currentProjection = _rightFrustum;
                CurrentOffest = new float3(Stereo3DParams.EyeDistance * 0.5f, 0, 0);
            }
               
        }

        //LookAt3D -Shift -- override/hide
        public override float4x4 LookAt3D(Stereo3DEye eye, float3 eyeV, float3 target, float3 up)
        {
            float4x4 shiftTranslation = float4x4.Identity;
            if (eye == Stereo3DEye.Left)
            {
                _rc.Projection = _leftFrustum;
                shiftTranslation = shiftTranslation*float4x4.CreateTranslation(Stereo3DParams.EyeDistance*0.5f, 0,0);
            }
            else
            {
                _rc.Projection = _rightFrustum;
                shiftTranslation = shiftTranslation * float4x4.CreateTranslation(Stereo3DParams.EyeDistance * -0.5f, 0,0);
            }
          //  var lookat = base.LookAt3D( eye, eyeV, target, up);
            var retval = shiftTranslation*float4x4.CreateTranslation(eyeV);
            return retval;

        }

        public void SetFrustums(RenderContext rc, float fovy, float aspectRatio, float zNear, float zFar)
        {
            Console.WriteLine("SetFrustums");
            _leftFrustum = CreatePerspectiveFieldOfViewFrustumShift(fovy, aspectRatio, zNear, zFar, Stereo3DParams.EyeDistance,15, true);
            _rightFrustum = CreatePerspectiveFieldOfViewFrustumShift(fovy, aspectRatio, zNear, zFar, Stereo3DParams.EyeDistance,15, false);
            _currentProjection = _leftFrustum;
        }


        /// <summary>
        /// Creates a left handed perspective projection matrix when using SteroCameraRig -> Frustrum shift / Off-axis.
        /// </summary>
        /// <param name="fovy">Angle of the field of view in the y direction (in radians)</param>
        /// <param name="aspect">Aspect ratio of the view (width / height)</param>
        /// <param name="zNear">Distance to the near clip plane</param>
        /// <param name="zFar">Distance to the far clip plane</param>
        /// <param name="IOD">intraocular distance</param>
        /// <param name="screenZero">distance to convergence plane</param>
        /// <param name="lefteye">defines if frustum is created for left odr right camera</param>
        /// <returns>A projection matrix that transforms camera space to raster space</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown under the following conditions:
        /// <list type="bullet">
        /// <item>fovy is zero, less than zero or larger than Math.PI</item>
        /// <item>aspect is negative or zero</item>
        /// <item>zNear is negative or zero</item>
        /// <item>zFar is negative or zero</item>
        /// <item>zNear is larger than zFar</item>
        /// </list>
        /// </exception>
        public float4x4 CreatePerspectiveFieldOfViewFrustumShift( float fovy, float aspect, float zNear, float zFar, float IOD, float screenZero, bool lefteye/* StereoRig*/)
        {
            if (fovy <= 0 || fovy > System.Math.PI)
                throw new ArgumentOutOfRangeException("fovy");
            if (aspect <= 0)
                throw new ArgumentOutOfRangeException("aspect");
            if (zNear <= 0)
                throw new ArgumentOutOfRangeException("zNear");
            if (zFar <= 0)
                throw new ArgumentOutOfRangeException("zFar");
            if (zNear >= zFar)
                throw new ArgumentOutOfRangeException("zNear");


            float shiftToLeftRight = (lefteye == true) ? 1 : -1;
            float shiftAmount = ((IOD * 0.5f) * zNear) / screenZero;
            float top = (float)System.Math.Tan(fovy * 0.5f) * zNear;
            float bottom = -top;
            float right = (aspect * top) + (shiftAmount * shiftToLeftRight);
            float left = (aspect * bottom) + (shiftAmount * shiftToLeftRight);

           
            float4x4 result;
            float4x4.CreatePerspectiveOffCenter(left, right, bottom, top, zNear, zFar, out result);
            return result;
        }
    }
}
