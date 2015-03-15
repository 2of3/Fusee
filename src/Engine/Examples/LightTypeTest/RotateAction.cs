using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Fusee.Engine;
using Fusee.Math;
using Fusee.SceneManagement;

namespace Examples.LightTypeTest
{
    public class RotateAction : ActionCode
    {
        private readonly float3 _rotSpeed;

        public RotateAction(float3 rotationSpeed)
        {
            _rotSpeed = rotationSpeed;
        }

        public override void Start()
        {
            transform.LocalEulerAngles = new float3(0, 0, 0);
        }

        public override void Update()
        {
            //transform.LocalEulerAngles -= _rotSpeed*(float) Time.Instance.DeltaTime;
            SceneManager.RC.DebugLine(transform.GlobalPosition, transform.Forward*10000, new float4(1, 1, 0, 1));
        }
    }
}
