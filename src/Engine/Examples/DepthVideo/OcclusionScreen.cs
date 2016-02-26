using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.DepthVideo
{
    
    public class OcclusionScreen
    {
        private Mesh _occlusionsScreen;
        private RenderContext _rc;

        private ShaderProgram _shaderPeogrammOd;
        private IShaderParam _textureParamDo;
        private ITexture _depthTexture;

        //private List<Image<Gray, byte>> _framesListDepth = new List<Image<Gray, byte>>();
        //private IEnumerator<Image<Gray, byte>> _framesListDepthEnumerator;

        private float4x4 _position;
        private float3 _scaleFactor;
        public OcclusionScreen(RenderContext rc, Mesh occlusionScreen, ShaderProgram shaderprogramm, float4x4 position, float3 scaleFactor)
        {
            _occlusionsScreen = occlusionScreen;
            _rc = rc;
            _shaderPeogrammOd = shaderprogramm;
            _textureParamDo = _shaderPeogrammOd.GetShaderParam("textureDepth");
            _position = position;
            _scaleFactor = scaleFactor;
        }

        public void Update(float4x4 newpos, ITexture depthTexture)
        {
            _position = newpos;
            _depthTexture = depthTexture;
        }

        public void RenderOcclusionScreen(float4x4 lookat, float4x4 rot)
        {
            _rc.SetShader(_shaderPeogrammOd);
            _rc.SetShaderParamTexture(_textureParamDo, _depthTexture);
            _rc.ModelView = lookat * rot* _position * float4x4.CreateRotationY((float)Math.PI)* float4x4.CreateScale(_scaleFactor);
            _rc.Render(_occlusionsScreen);
        }
    }
}
