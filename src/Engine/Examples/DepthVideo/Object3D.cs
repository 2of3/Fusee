using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.DepthVideo
{
    public class Object3D
    {
        public float3 Position { get; set; }
        public float3 Rotation { get; set; }
        public Mesh Mesh { get; set; }
        public float ScaleFactor { get; set; }
        public float Brightness { get; set; }
        private CurrentShaderMaterial _currentMaterial;

        private RenderContext _rc;
  
        // public float4x4 Position { get; set; }

        private struct CurrentShaderMaterial
        {
            public ShaderProgram ShaderProgram;
            public IShaderParam ShaderTextureParam;
            public IShaderParam ShaderColorParam;
            public float4  MatColor;
            public ITexture MatTexture;

        }

        public Object3D(RenderContext rc, float3 position, float3 rotation, Mesh mesh, float scalefactor, float brightness)
        {
            _currentMaterial = new CurrentShaderMaterial() { ShaderProgram = null, ShaderTextureParam = null, ShaderColorParam = null, MatTexture = null, MatColor = float4.Zero};
            Position = position;
            Rotation = rotation;
            Mesh = mesh;
            ScaleFactor = scalefactor;
            Brightness = brightness;
            _rc = rc;
        }


        public void SimpleTextureMaterial(ShaderProgram shaderProgram, IShaderParam texruteParam, IShaderParam colorParam, ITexture texture, float4 color)
        {
            _currentMaterial.ShaderProgram = shaderProgram;
            _currentMaterial.ShaderTextureParam = texruteParam;
            _currentMaterial.ShaderColorParam = colorParam;
            _currentMaterial.MatColor = color;
            _currentMaterial.MatTexture = texture;
        }

        public void Render(float4x4 mtxCam)
        {
            if (_currentMaterial.ShaderProgram != null)
            {
                _rc.SetShader(_currentMaterial.ShaderProgram);
                _rc.SetShaderParam(_currentMaterial.ShaderColorParam, new float4(new float3(_currentMaterial.MatColor.x, _currentMaterial.MatColor.y, _currentMaterial.MatColor.z), Brightness));
                _rc.SetShaderParamTexture(_currentMaterial.ShaderTextureParam, _currentMaterial.MatTexture);
                _rc.ModelView = mtxCam *  float4x4.CreateTranslation(Position) * float4x4.CreateRotationX(Rotation.x) *float4x4.CreateRotationY(Rotation.y) * float4x4.CreateRotationZ(Rotation.z) * float4x4.CreateScale(ScaleFactor);
                _rc.Render(Mesh);
            }
            else
            {
                Console.WriteLine("Woops");
            }
        }
    }
}
