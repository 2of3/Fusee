using System.Security.Cryptography.X509Certificates;
using Fusee.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusee.Math;


namespace Fusee.Engine.SimpleScene
{

    public class ScenePicker
    {

        private PickingContext _pc;
        private RenderContext _rc;
        private Dictionary<MeshContainer, Mesh> _meshMap;
        private float4x4 _modelView;

        public ScenePicker(RenderContext rc)
        {
            // Create picking context
            _pc = new PickingContext(rc, PickType.Ray, false);
            _meshMap = new Dictionary<MeshContainer, Mesh>();
            _rc = rc;
        }

        public IEnumerable<PickResultSet> Pick(SceneContainer sc, Point pickPos)
        {
            // Initialize picking context
            _pc.Pick(pickPos);
            _modelView = _rc.ModelView;
            Traverse(sc);
            _pc.Tick();
            return _pc.PickResults;
        }

        private void Traverse(SceneContainer sc)
        {
            Traverse(sc.Children);
        }

        private void Traverse(IEnumerable<SceneObjectContainer> children)
        {
            if (children == null)
            {
                return;
            }

            foreach (SceneObjectContainer soc in children)
            {
                _modelView = _modelView * soc.Transform.Matrix();

                if (soc.Mesh != null && soc.Mesh.Vertices != null)
                {
                    Mesh mesh;
                    if (!_meshMap.TryGetValue(soc.Mesh, out mesh))
                    {
                        mesh = MakeMesh(soc.Mesh);
                        _meshMap[soc.Mesh] =  mesh;
                    }
                    _pc.AddPickableObject(soc, mesh, soc.Name, float4x4.Identity, _modelView); //TODO Name des jew. soc?
                }

                float4x4 currentModelView = _modelView;
                Traverse(soc.Children);
                _modelView = currentModelView;
            }
        }

        public static Mesh MakeMesh(MeshContainer mc)
        {
            Mesh rm;
            rm = new Mesh()
            {
                Colors = null,
                Normals = mc.Normals,
                UVs = mc.UVs,
                Vertices = mc.Vertices,
                Triangles = mc.Triangles
            };
            return rm;
        }

    }

}
