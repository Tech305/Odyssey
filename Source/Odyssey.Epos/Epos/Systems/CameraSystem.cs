﻿#region License

// Copyright © 2013-2014 Avengers UTD - Adalberto L. Simeone
// 
// The Odyssey Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License Version 3 as published by
// the Free Software Foundation.
// 
// The Odyssey Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details at http://gplv3.fsf.org/

#endregion

#region Using Directives

using Odyssey.Engine;
using Odyssey.Epos.Components;
using Odyssey.Epos.Messages;
using SharpDX.Mathematics;

#endregion

namespace Odyssey.Epos.Systems
{
    public class PerspectiveCameraSystem : UpdateableSystemBase
    {
        public PerspectiveCameraSystem()
            : base(Selector.All(typeof (PositionComponent), typeof (OrientationComponent), typeof (CameraComponent), typeof (UpdateComponent)))
        {
        }

        public override void Start()
        {
            Messenger.Register<EntityChangeMessage>(this);
        }

        public override void Stop()
        {
            Messenger.Unregister<EntityChangeMessage>(this);
        }
        
        public override bool BeforeUpdate()
        {
            // Entity change
            while (MessageQueue.HasItems<EntityChangeMessage>())
            {
                EntityChangeMessage mEntity = MessageQueue.Dequeue<EntityChangeMessage>();
                var entity = mEntity.Source;
                var camera = entity.GetComponent<CameraComponent>();
                
                if (mEntity.Action == UpdateType.Add)
                {
                    ResetCamera(entity);
                    Messenger.Send(new CameraMessage(entity, camera, UpdateType.Add));
                }
                else if (mEntity.Action == UpdateType.Remove)
                    Messenger.Send(new CameraMessage(entity, camera, UpdateType.Remove));
            }
            return base.BeforeUpdate();
        }


        public override void Process(ITimeService time)
        {
            foreach (Entity entity in Entities)
            {
                var cUpdate = entity.GetComponent<UpdateComponent>();
                if (!cUpdate.RequiresUpdate)
                    continue;
                var cPosition = entity.GetComponent<PositionComponent>();
                var cRotation = entity.GetComponent<OrientationComponent>();
                var cCamera = entity.GetComponent<CameraComponent>();
                cCamera.View = Matrix.Translation(-cPosition.Position)* Matrix.RotationQuaternion(cRotation.Orientation);
            }
        }

        private void ResetCamera(Entity entity)
        {
            var deviceSettings = entity.Scene.Services.GetService<IDirectXDeviceSettings>();
            float aspectRatio = deviceSettings.PreferredBackBufferWidth/(float) deviceSettings.PreferredBackBufferHeight;
            var cCamera = entity.GetComponent<CameraComponent>();
            var cPosition = entity.GetComponent<PositionComponent>();
            var cRotation = entity.GetComponent<OrientationComponent>();
            
            TargetComponent cTarget;
            cCamera.Viewport = new ViewportF(0, 0, deviceSettings.PreferredBackBufferWidth,
                deviceSettings.PreferredBackBufferHeight);
            cCamera.Projection = Matrix.PerspectiveFovRH(cCamera.FieldOfView, aspectRatio, cCamera.NearClip, cCamera.FarClip);
            entity.TryGetComponent(out cTarget);

            Vector3 pFocus = cTarget != null ? cTarget.Location : Vector3.Zero;
            cRotation.Orientation = Quaternion.LookAtRH(cPosition.Position, pFocus, cCamera.Up);
            cCamera.View = Matrix.LookAtRH(cPosition.Position, pFocus, cCamera.Up);

            var cUpdate = entity.GetComponent<UpdateComponent>();
            if (cUpdate.UpdateFrequency == UpdateFrequency.Static)
                cUpdate.RequiresUpdate = false;
        }
    }
}