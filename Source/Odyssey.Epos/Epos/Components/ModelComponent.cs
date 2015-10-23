﻿using Odyssey.Epos.Messages;
using Odyssey.Graphics.Models;
using System;

namespace Odyssey.Epos.Components
{
    [OptionalComponent(typeof(ShaderComponent), typeof(PostProcessComponent))]
    [RequiredComponent(typeof(PositionComponent))]
    [RequiredComponent(typeof(TransformComponent))]
    public sealed class ModelComponent : ContentComponent
    {
        private Model model;
        [PropertyUpdate(UpdateAction.Register)]
        public Model Model {
            get
            {
                return model;
            }
            private set
            {
                if (model != value)
                {
                    model = value;
                    RaisePropertyChanged();
                }
            }
        }

        public override bool IsInited { get { return Model != null; } }
        
        public ModelComponent()
            : base(ComponentTypeManager.GetType<ModelComponent>())
        {
        }

        public override void Initialize()
        {
            if (IsProcedurallyGenerated)
                return;
            Model = Content.Load<Model>(AssetName);
        }

        protected override void OnReceiveMessage(MessageEventArgs e)
        {
            base.OnReceiveMessage(e);

            var mContent = e.Message as ContentMessage<Model>;
            if (mContent == null)
                throw new InvalidOperationException($"Message of type '{e.Message.GetType()}' is not supported");
            Model = mContent.Content;
        }
    }
}
