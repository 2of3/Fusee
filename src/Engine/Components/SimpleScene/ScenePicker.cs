using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusee.Math;
using Fusee.Serialization;
using ProtoBuf;

namespace Fusee.Engine.SimpleScene
{
    public struct PickResult
    {
        public SceneNodeContainer Node;
        public MeshComponent Mesh;
        public int Triangle;
        public float WA, WB, WC;

        // TODO: Implement
        public float3 WorldPos
        { get { throw new NotImplementedException(); } }

        // TODO: Implement
        public float3 ModelPos
        { get { throw new NotImplementedException(); } }

        // TODO: Implement
        public float4 ScreenPos
        { get { throw new NotImplementedException();} }
     }


    public class ScenePicker : Viserator<PickResult, ScenePicker.PickingState>
    {
        public class PickingState : VisitorState
        {
            public delegate bool TriangleInPointTest(
                float2 p, float2 a, float2 b, float2 c, out float u, out float v, out float w);
            private CollapsingStateStack<float4x4> _model = new CollapsingStateStack<float4x4>();
            private CollapsingStateStack<float4x4> _view = new CollapsingStateStack<float4x4>();
            private CollapsingStateStack<float4x4> _projection = new CollapsingStateStack<float4x4>();
            private CollapsingStateStack<TriangleInPointTest> _triInPointTest = new CollapsingStateStack<TriangleInPointTest>();

            public float4x4 Model
            {
                set { _model.Tos = value; }
                get { return _model.Tos; }
            }

            public float4x4 View
            {
                set { _view.Tos = value; }
                get { return _view.Tos; }
            }

            public float4x4 Projection
            {
                set { _projection.Tos =  value; }
                get { return _projection.Tos; }
            }

            public TriangleInPointTest TriInPointTest
            {
                set { _triInPointTest.Tos = value; }
                get { return _triInPointTest.Tos; }
            }

            public PickingState()
            {
                RegisterState(_model);
                RegisterState(_view);
                RegisterState(_projection);
                RegisterState(_triInPointTest);
                _triInPointTest.Tos = MathHelper.
            }
        }

        public Point PickPos
        {
            set { _fPickPos = new float4(value.x, value.y, 0.0f, 1.0f);}
            get { return new Point{x= (int) _fPickPos.x, y = (int) _fPickPos.y, z = 0};}
        }
        private float4 _fPickPos;


        #region Visitors
        [VisitMethod]
        public void PickTransform(TransformComponent transform)
        {
            State.Model *= transform.Matrix();
        }

        [VisitMethod]
        public void PickMesh(MeshComponent meshComponent)
        {
            float4x4 mvp = State.Model*State.View*State.Projection;
            for (int iTri = 0; iTri < meshComponent.Triangles.Length; iTri+=3)
            {
                float4 a = mvp*new float4(meshComponent.Vertices[iTri+0], 1);
                a /= a.w;
                float4 b = mvp*new float4(meshComponent.Vertices[iTri+1], 1);
                b /= b.w;
                float4 c = mvp*new float4(meshComponent.Vertices[iTri+2], 1);
                c /= c.w;
                
                if ()

              if (triangle is hit by pickpos)
              {
                YieldItem(new PickResult
                     {
                         Mesh = meshComponent,
                         Node = CurrentNode,
                         Triangle = TODO,
                         WA = TODO,
                         WB = TODO,
                         WC = TODO
                     });
              }
            }
             * */
        }
        #endregion
 
    }
}
