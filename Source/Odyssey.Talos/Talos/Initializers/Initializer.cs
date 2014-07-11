﻿using Odyssey.Engine;
using Odyssey.Graphics.Effects;
using Odyssey.Graphics.Shaders;
using Odyssey.Utilities.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Odyssey.Talos.Initializers
{
    public abstract class Initializer<TSource> : IInitializer
    {
        protected static readonly SelectorDelegate DefaultSelector = cb => true;
        protected static readonly SelectorDelegate InstanceSelector = cb => cb.UpdateFrequency == UpdateType.InstanceFrame || cb.UpdateFrequency == UpdateType.InstanceStatic;
        protected static readonly SelectorDelegate StaticSelector = cb => cb.UpdateFrequency == UpdateType.SceneStatic || cb.UpdateFrequency == UpdateType.SceneFrame;
        private DirectXDevice device;

        protected Initializer(IEnumerable<EngineReference> acceptedReferences)
        {
            AcceptedReferences = acceptedReferences.ToArray();
        }

        public EngineReference[] AcceptedReferences { get; private set; }

        public virtual void Initialize(DirectXDevice device, Effect effect, TSource source, InitializerParameters parameters)
        {
            LogEvent.Engine.Info("[{0}, {1}] in {2}", GetType().Name, parameters.EntityId, parameters.Technique.Name);
            this.device = device;
            InitializeConstantBuffers(effect, source, parameters);
        }

        public abstract void SetupInitialization(ShaderInitializer initializer);

        protected abstract IEnumerable<IParameter> CreateParameter(ConstantBufferDescription cbParent, TSource source, int parameterIndex, EngineReference reference, InitializerParameters initializerParameters);

        protected abstract bool ValidateConstantBuffer(ConstantBufferDescription cb);

        private void InitializeConstantBuffers(Effect effect, TSource source, InitializerParameters parameters)
        {
            var referenceTable = from shaderObject in parameters.Technique.Shaders
                                 from cb in shaderObject.ConstantBuffers
                                 where parameters.Selector(cb) && ValidateConstantBuffer(cb)
                                 select cb;

            string effectName = effect.Name;
            foreach (var cbDesc in referenceTable)
            {
                if (!effect[cbDesc.ShaderType].HasConstantBuffer(cbDesc.Index, effectName, parameters.EntityId))
                    effect[cbDesc.ShaderType].AddConstantBuffer(parameters.EntityId, new ConstantBuffer(device, cbDesc, effectName));

                ConstantBuffer cb = effect[cbDesc.ShaderType].GetConstantBuffer(cbDesc.Index, effectName, parameters.EntityId);

                var validReferences = from kvp in cbDesc.References
                                      let reference = kvp.Value
                                      where AcceptedReferences.Contains(reference)
                                      select kvp;

                foreach (var kvp in validReferences)
                {
                    var reference = kvp.Value;
                    if (cbDesc.UpdateFrequency != UpdateType.InstanceFrame && cbDesc.IsParsed(effectName, reference))
                        continue;

                    var effectParameters = CreateParameter(cbDesc, source, kvp.Key, reference, parameters);

                    foreach (IParameter parameter in effectParameters)
                    {
                        cb.AddParameter(kvp.Key, parameter);
                    }
                    cbDesc.MarkAsParsed(effectName, reference);
                }
            }
        }
    }
}