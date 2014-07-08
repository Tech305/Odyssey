﻿using Odyssey.Graphics;
using Odyssey.Graphics.Effects;
using SharpDX.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Odyssey.Tools.ShaderGenerator.Shaders.Structs
{
    [DataContract]
    public partial class ConstantBuffer : Struct
    {
        [DataMember]
        private Dictionary<EngineReference, ShaderReference> references;

        [DataMember]
        public UpdateType UpdateType { get; set; }

        public ConstantBuffer()
        {
            Type = Shaders.Type.ConstantBuffer;
            Index = 0;
            references = new Dictionary<EngineReference, ShaderReference>();
        }

        public IEnumerable<ShaderReference> References { get { return references.Values; } }

        public void SetReference(ShaderReference reference)
        {
            Contract.Requires<ArgumentException>(reference.Type == ReferenceType.Engine);
            EngineReference cbReference = (EngineReference)reference.Value;
            if (!references.ContainsKey(cbReference))
                references.Add(cbReference, reference);
            reference.Index = references.Count - 1;
        }

        public override void Add(IVariable variable)
        {
            base.Add(variable);
            if (variable.ShaderReference != null)
                SetReference(variable.ShaderReference);
        }

        public override string Definition
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(string.Format("{0} {1} : register({2})", Mapper.Map(Type), Name, GetRegister(this)));
                sb.AppendLine("{");
                foreach (var variable in Variables)
                {
                    sb.AppendFormat("{0}", variable.Type == Type.Struct ? ((IStruct)variable).Declaration : variable.Definition);
                }
                sb.AppendLine("};");
                return sb.ToString();
            }
        }
    }
}