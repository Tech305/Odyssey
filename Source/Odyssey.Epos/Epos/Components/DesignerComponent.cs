﻿using System.Collections.Generic;
using Odyssey.Content;
using Odyssey.Epos.Messages;
using Odyssey.Graphics.Drawing;
using Odyssey.Graphics.Models;

namespace Odyssey.Epos.Components
{
    public class DesignerComponent : ContentComponent
    {
        private bool isInited;
        private List<IDesignerInstruction> instructions;

        public override bool IsInited { get { return isInited; } }

        public DesignerComponent() : base(ComponentTypeManager.GetType<DesignerComponent>()) { }

        public List<IDesignerInstruction> Instructions
        {
            get { return instructions; }
            set
            {
                if (instructions != value)
                {
                    instructions = value;
                    RaisePropertyChanged();
                }
            }
        }

        public override bool Validate()
        {
            return true;
        }

        public override void Initialize()
        {
            Designer designer = new Designer(Services);
            designer.Begin();
            foreach (var instruction in instructions)
                instruction.Execute(designer);
            designer.End();
            isInited = true;

            var model = designer.Result;
            model.Name = Name + ".Mesh";
            AssetName = model.Name;
            Services.GetService<IAssetProvider>().Store(model.Name, model);
            Messenger.SendToEntity<ModelComponent>(new ContentMessage<Model>(Owner, AssetName, model), Owner);
        }
    }
}
