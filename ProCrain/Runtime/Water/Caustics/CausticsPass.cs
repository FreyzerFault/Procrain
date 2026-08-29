//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━																												
// Copyright 2020, Alexander Ameye, All rights reserved.
// https://alexander-ameye.gitbook.io/stylized-water/
//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━	

#if UNIVERSAL_RENDERER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Procrain.Caustics
{
    public class CausticsPass : ScriptableRenderPass
    {
        private const string profilerTag = "Caustics Pass";

        private const float BIAS = 0.1f;
        private static Mesh mesh;
        private readonly float waterLevel;

        public Material causticsMaterial;

        public CausticsPass(float waterLevel) => this.waterLevel = waterLevel;
        
        private class PassData
        {
            public Camera camera;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;

            if (camera.cameraType == CameraType.Preview || !causticsMaterial)
                return;

            using (var builder = renderGraph.AddUnsafePass<PassData>(
                       profilerTag,
                       out var passData))
            {
                passData.camera = camera;

                // Como dibujamos directamente sobre el framebuffer,
                // evitamos que Render Graph considere el pass inútil.
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                {
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    ExecuteCaustics(data, cmd);
                });
            }
        }

        private void ExecuteCaustics(PassData data, CommandBuffer cmd)
        {
            var cam = data.camera;

            var sunMatrix = RenderSettings.sun != null
                ? RenderSettings.sun.transform.localToWorldMatrix
                : Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.Euler(-45f, 45f, 0f),
                    Vector3.one);

            causticsMaterial.SetMatrix("_MainLightDirection", sunMatrix);

            if (!mesh)
                mesh = GenerateQuad(1000f);

            var position = cam.transform.position;
            position.y = cam.transform.position.y > waterLevel
                ? waterLevel
                : cam.transform.position.y - BIAS;

            var matrix = Matrix4x4.TRS(
                position,
                Quaternion.identity,
                Vector3.one);

            cmd.DrawMesh(mesh, matrix, causticsMaterial, 0, 0);
        }
        private static Mesh GenerateQuad(float size)
        {
            var m = new Mesh();

            size *= 0.5f;

            var verts = new[]
            {
                new Vector3(-size, 0f, -size),
                new Vector3(size, 0f, -size),
                new Vector3(-size, 0f, size),
                new Vector3(size, 0f, size)
            };

            var tris = new[]
            {
                0, 2, 1,
                2, 3, 1
            };

            m.vertices = verts;
            m.triangles = tris;

            return m;
        }
    }
}
#endif
