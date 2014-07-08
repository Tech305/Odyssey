﻿using Odyssey.Engine;
using Odyssey.Graphics.Effects;
using Odyssey.Graphics.Shaders;
using Odyssey.Tools.ShaderGenerator.Shaders.Methods;
using Odyssey.Tools.ShaderGenerator.Shaders.Nodes;
using Odyssey.Tools.ShaderGenerator.Shaders.Nodes.Functions;
using Odyssey.Tools.ShaderGenerator.Shaders.Nodes.Math;
using Odyssey.Tools.ShaderGenerator.Shaders.Nodes.Operators;
using Odyssey.Tools.ShaderGenerator.Shaders.Structs;
using ConstantBuffer = Odyssey.Tools.ShaderGenerator.Shaders.Structs.ConstantBuffer;

namespace Odyssey.Tools.ShaderGenerator.Shaders.Techniques
{
    public class GlowPS : Shader
    {
        public GlowPS()
        {
            Name = "GlowPS";
            Type = ShaderType.Pixel;
            KeyPart = new TechniqueKey(ps: PixelShaderFlags.Diffuse | PixelShaderFlags.DiffuseMap);
            FeatureLevel = FeatureLevel.PS_4_0_Level_9_1;
            EnableSeparators = true;
            var inputStruct = SpriteVS.VSOut;
            inputStruct.Name = "input";

            InputStruct = inputStruct;
            OutputStruct = Struct.PixelShaderOutput;
            Texture tDiffuse = Texture.Diffuse;
            Texture tGlow = new Texture { Name = "tGlow", Type = Shaders.Type.Texture2D };
            Variable sLinearWrap = Sampler.MinMagMipLinearWrap;
            var cbFrame = CBFrame;

            Add(sLinearWrap);
            Add(tDiffuse);
            Add(tGlow);
            Add(cbFrame);

            TextureSampleNode nTexSample = new TextureSampleNode
            {
                Coordinates = new ReferenceNode { Value = InputStruct[Param.SemanticVariables.Texture] },
                Texture = tDiffuse,
                Sampler = sLinearWrap,
                IsVerbose = true,
                Output = new Vector { Name = "cDiffuse", Type = Shaders.Type.Float4 }
            };

            TextureSampleNode nGlowSample = new TextureSampleNode
            {
                Coordinates = new ReferenceNode { Value = InputStruct[Param.SemanticVariables.Texture] },
                Texture = tGlow,
                Sampler = sLinearWrap,
                IsVerbose = true,
                Output = new Vector { Name = "cGlow", Type = Shaders.Type.Float4 }
            };

            UnaryFunctionNode nSaturate = new UnaryFunctionNode
            {
                Input1 = new AdditionNode
                {
                    Input1 = nTexSample,
                    Input2 = new MultiplyNode
                    {
                        Input1 = nGlowSample,
                        Input2 = new ReferenceNode { Value = cbFrame[Param.Floats.GlowStrength] },
                        Parenthesize = true
                    }
                },
                Function = HlslIntrinsics.Saturate
            };

            Result = new PSOutputNode
            {
                FinalColor = nSaturate,
                Output = OutputStruct
            };
        }

        public static ConstantBuffer CBFrame
        {
            get
            {
                var cbFrame = new ConstantBuffer
                {
                    Name = Param.ConstantBuffer.PerFrame,
                    UpdateType = UpdateType.InstanceFrame,
                };
                cbFrame.Add(Float.GlowStrength);
                return cbFrame;
            }
        }
    }
}