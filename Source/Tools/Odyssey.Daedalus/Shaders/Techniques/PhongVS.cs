﻿using Odyssey.Daedalus.Shaders.Nodes;
using Odyssey.Daedalus.Shaders.Nodes.Math;
using Odyssey.Daedalus.Shaders.Nodes.Operators;
using Odyssey.Daedalus.Shaders.Structs;
using Odyssey.Engine;
using Odyssey.Graphics.Effects;
using Odyssey.Graphics.Shaders;
using ConstantBuffer = Odyssey.Daedalus.Shaders.Structs.ConstantBuffer;

namespace Odyssey.Daedalus.Shaders.Techniques
{
    public class PhongInstanceVS : Shader
    {
        public PhongInstanceVS()
        {
            Name = "PhongInstanceVS";
            Type = ShaderType.Vertex;
            KeyPart = new TechniqueKey(vs: VertexShaderFlags.Position | VertexShaderFlags.Normal | VertexShaderFlags.TextureUV | VertexShaderFlags.InstanceWorld);
            FeatureLevel = FeatureLevel.VS_4_0_Level_9_3;
            EnableSeparators = true;
            var instanceStruct = Struct.VertexPositionNormalTexture;
            instanceStruct.Add(Matrix.EntityInstanceWorld);

            InputStruct = instanceStruct;
            OutputStruct = Struct.VertexPositionNormalTextureOut;

            var cbFrame = ConstantBuffer.CBPerFrame;
            Add(cbFrame);

            var position = InputStruct[Param.SemanticVariables.ObjectPosition];
            var normal = InputStruct[Param.SemanticVariables.Normal];
            var texture = InputStruct[Param.SemanticVariables.Texture];

            var nV3toV4 = new CastNode
            {
                Input = new SwizzleNode { Input = new ReferenceNode {Value= position}, Swizzle = new[] { Swizzle.X, Swizzle.Y, Swizzle.Z, Swizzle.Null } },
                Output = new Vector { Type = Shaders.Type.Float4, Name = "vPosition" },
                Mask = new[] { "0", "0", "0", "1" },
                IsVerbose = true
            };

            var mWorld = new ReferenceNode
            {
                Value = InputStruct[Param.SemanticVariables.InstanceWorld],
                Output = new Matrix {Type = Shaders.Type.Matrix, Name = "mWorld",},
                IsVerbose = true
                
            };

            var m1m2 = new MatrixMultiplyNode
            {
                Input1 = mWorld,
                Input2 = new ReferenceNode { Value = Matrix.CameraView },
            };

            var mulWVP = new MatrixMultiplyNode
            {
                Input1 = m1m2,
                Input2 = new ReferenceNode { Value = Matrix.CameraProjection },
                IsVerbose = true
            };

            var mulPosWVP = new MatrixMultiplyNode
            {
                Input1 = nV3toV4,
                Input2 = mulWVP,
            };
            
            var mulWP = new MatrixMultiplyNode
            {
                Input1 = nV3toV4,
                Input2 = mWorld,
            };

            var mulNormal = new MatrixMultiplyNode
            {
                Input1 = new ReferenceNode { Value = normal },
                Input2 = CastNode.CastWorldFloat3x3((Matrix)mWorld.Output, "mWorld3x3")
            };

            Result = new PhongVSOutputNode
            {
                Position = mulPosWVP,
                WorldPosition = mulWP,
                Normal = mulNormal,
                Texture = new ReferenceNode { Value = texture },
                Output = OutputStruct
            };
        }

    }

    public class PhongVS : Shader
    {
        public PhongVS()
        {
            Name = "PhongVS";
            Type = ShaderType.Vertex;
            KeyPart = new TechniqueKey(vs: VertexShaderFlags.Position | VertexShaderFlags.Normal | VertexShaderFlags.TextureUV);
            FeatureLevel = FeatureLevel.VS_4_0_Level_9_1;
            EnableSeparators = true;
            InputStruct = Struct.VertexPositionNormalTexture;
            OutputStruct = Struct.VertexPositionNormalTextureOut;

            var cbFrame = ConstantBuffer.CBPerFrame;
            var cbInstance = CBPerInstance;

            Add(cbFrame);
            Add(cbInstance);

            var position = InputStruct[Param.SemanticVariables.ObjectPosition];
            var normal = InputStruct[Param.SemanticVariables.Normal];
            var texture = InputStruct[Param.SemanticVariables.Texture];

            var nV3toV4 = new CastNode
            {
                Input = new SwizzleNode { Input = new ReferenceNode { Value = position }, Swizzle = new[] { Swizzle.X, Swizzle.Y, Swizzle.Z, Swizzle.Null } },
                Output = new Vector { Type = Shaders.Type.Float4, Name = "vPosition" },
                Mask = new[] { "0", "0", "0", "1" },
                IsVerbose = true
            };

            var mulPosWVP = new MatrixMultiplyNode
            {
                Input1 = nV3toV4,
                Input2 = MatrixMultiplyNode.WorldViewProjection,
            };

            var mulWP = new MatrixMultiplyNode
            {
                Input1 = nV3toV4,
                Input2 = new ReferenceNode { Value = Matrix.EntityWorld },
            };

            var mulNormal = new MatrixMultiplyNode
            {
                Input1 = new ReferenceNode { Value = normal },
                Input2 = CastNode.WorldInverseTransposeFloat3x3
            };

            Result = new PhongVSOutputNode
            {
                Position = mulPosWVP,
                WorldPosition = mulWP,
                Normal = mulNormal,
                Texture = new ReferenceNode { Value = texture },
                Output = OutputStruct
            };
        }

        public static ConstantBuffer CBPerInstance
        {
            get
            {
                var cbInstance = ConstantBuffer.CBPerInstance;
                cbInstance.Add(Matrix.EntityWorldInverseTranspose);
                return cbInstance;
            }
        }
    }

    public class PhongCubeMapVS : PhongVS
    {
        public PhongCubeMapVS()
        {
            Name = "PhongCubeMapVS";
            KeyPart = new TechniqueKey(vs: VertexShaderFlags.Position | VertexShaderFlags.Normal | VertexShaderFlags.TextureUVW);
            InputStruct = Struct.VertexPositionNormalTextureUVW;
            OutputStruct = VSOut;

            var outputNode = (PhongVSOutputNode)Result;
            outputNode.Texture = new ReferenceNode { Value = InputStruct[Param.SemanticVariables.Texture] };
        }

        public static Struct VSOut
        {
            get
            {
                var vpt = new Struct()
                {
                    CustomType = CustomType.VSOut,
                    Name = "output",
                };
                vpt.Add(Vector.ClipPosition);
                vpt.Add(Vector.WorldPosition4);
                vpt.Add(Vector.Normal);
                vpt.Add(Vector.TextureUVW);
                return vpt;
            }
        }
    }
}
