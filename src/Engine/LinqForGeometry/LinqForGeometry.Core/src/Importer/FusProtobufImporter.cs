using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fusee.LFG.Core.Importer;
using Fusee.Engine;
using Fusee.Math;

namespace Fusee.LFG.Core.src.Importer
{
    class FusProtobufImporter : ILfgImporter
    {
        private List<GeoFace> _GeoFaces;
        private String _Path = "";
        private FuseeSerializer _Serializer;
        private Mesh _Mesh;

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
            _Serializer = new FuseeSerializer();
            using (var file = File.OpenRead(path))
            {
                _Mesh = _Serializer.Deserialize(file, null, typeof(Mesh)) as Mesh;
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

                _GeoFaces.Add(gf);
            }        
        }


    }
}
