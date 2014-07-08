﻿using Odyssey.Engine;
using Odyssey.Graphics.Effects;
using Odyssey.Graphics.Shaders;
using Odyssey.Tools.ShaderGenerator.Shaders.Nodes;
using Odyssey.Tools.ShaderGenerator.Shaders.Nodes.Operators;
using Odyssey.Tools.ShaderGenerator.Shaders.Nodes.Math;
using Odyssey.Tools.ShaderGenerator.Shaders.Structs;
using System.Runtime.Serialization;
using ConstantBuffer = Odyssey.Tools.ShaderGenerator.Shaders.Structs.ConstantBuffer;

namespace Odyssey.Tools.ShaderGenerator.Shaders.Techniques
{
    [DataContract]
    [VertexShader(VertexShaderFlags.Position)]
    [VertexShader(VertexShaderFlags.Normal)]
    [VertexShader(VertexShaderFlags.TextureUV)]
    public class PhongShadowsVS : PhongVS
    {
        public PhongShadowsVS()
        {
            Name = "PhongShadowsVS";
            KeyPart = new TechniqueKey(vs: VertexShaderFlags.Position | VertexShaderFlags.Normal | VertexShaderFlags.TextureUV);
            Clear();
            PhongVSOutputNode oldOutput = (PhongVSOutputNode)Result;
            InputStruct = Struct.VertexPositionNormalTexture;
            OutputStruct = VSOut;

            Add(CBStatic);
            Add(ConstantBuffer.CBPerFrame);
            Add(CBPerInstance);

            MatrixMultiplyNode mShadow = new MatrixMultiplyNode
            {
                Input1 = new ReferenceNode { Value = InputStruct[Param.SemanticVariables.Position] },
                Input2 = MatrixMultiplyNode.LightWorldViewProjection
            };

            Result = new PhongShadowsVSOutputNode
            {
                Position = oldOutput.Position,
                Normal = oldOutput.Normal,
                WorldPosition = oldOutput.WorldPosition,
                Texture = oldOutput.Texture,
                ShadowProjection = mShadow,
                Output = OutputStruct
            };
        }

        public static Struct VSOut
        {
            get
            {
                Struct vpt = new Struct() 
                {
                    CustomType = CustomType.VSOut,
                    Name = "output",
                };
                vpt.Add(Vector.ClipPosition); 
                vpt.Add(Vector.WorldPosition4);
                vpt.Add(Vector.Normal);
                vpt.Add(Vector.TextureUV);
                vpt.Add(Vector.ShadowProjection);
                return vpt;
            }
        }

        public static Struct CBStatic
        {
            get
            {
                ConstantBuffer cbStatic = new ConstantBuffer
                {
                    Name = Param.ConstantBuffer.Static,
                    UpdateType = UpdateType.SceneStatic,
                };

                cbStatic.Add(Struct.PointLightVS);
                cbStatic.Add(Matrix.LightView);
                cbStatic.Add(Matrix.LightProjection);
                return cbStatic;
            }
        }

        
    }


}
