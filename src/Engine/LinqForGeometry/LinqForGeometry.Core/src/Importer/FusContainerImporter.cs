using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Fusee.Engine;
using Fusee.Engine.SimpleScene;
using Fusee.LFG.Core.Importer;
using Fusee.Math;
using Fusee.Serialization;

namespace Fusee.LFG.Core.src.Importer
{
    class FusContainerImporter : ILfgImporter
    {
        private List<GeoFace> _GeoFaces;
        private String _Path = "";
        private Mesh _Mesh;
        private SceneContainer _Scene;

        private List<float3> _Vertices;
        private List<float2> _UVList;
        private List<float3> _Normals;

        public List<GeoFace> TmpData { get { return _GeoFaces; } }
        public string Filepath { get { return _Path; } }

        
        public List<GeoFace> LoadAsset(string path)
        {
            _Path = path;
            LoadProtobufFile(path);

            return TmpData;
        }

        private void LoadProtobufFile(string path)
        {
            var _serializer = new Serializer();
            using (var file = File.OpenRead(path))
            {
                _Scene = _serializer.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
                _Mesh = SceneRenderer.MakeMesh(_Scene.Children[0]);
            }
            _GeoFaces = new List<GeoFace>();
            ConvertMesh();
        }

        private void ConvertMesh()
        {
            int countTriangles = _Mesh.Triangles.Count();
            int realTriangles = countTriangles / 3; // TODO: Support for quads?

            for (int i = 0; i < realTriangles; i++)
            {
                GeoFace gf = new GeoFace();
                gf._Vertices = new List<float3>();
                gf._Normals = new List<float3>();
                gf._UV = new List<float2>();

                for (int j = 0; j < 3; j++)
                {
                    uint vertID = _Mesh.Triangles[_GeoFaces.Count * 3 + j];

                    gf._Vertices.Add(_Mesh.Vertices[vertID]);
                    gf._UV.Add(_Mesh.UVs[vertID]);
                    gf._Normals.Add(_Mesh.Normals[vertID]);
                }

                gf._Vertices.Reverse();
                //gf._Normals.Reverse();
                //gf._UV.Reverse();

                _GeoFaces.Add(gf);
            }    
        }


    }
}
