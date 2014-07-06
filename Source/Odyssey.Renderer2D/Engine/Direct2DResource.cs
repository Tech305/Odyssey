﻿#if !WP8

using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace Odyssey.Engine
{
    public class Direct2DResource : Component
    {
        /// <summary>
        /// The attached Direct3D11 resource to this instance.
        /// </summary>
        protected Resource Resource { get; set; }

        public Direct2DDevice Device { get; internal set; }

        protected Direct2DResource(string name)
            : base(name)
        { }

        protected Direct2DResource(Direct2DDevice device)
            : this(device, null)
        {
        }

        protected Direct2DResource(Direct2DDevice device, string name)
            : base(name)
        {
            Contract.Requires<ArgumentNullException>(device != null, "device");

            Device = device;
        }

        /// <summary>
        /// Initializes the specified device local.
        /// </summary>
        /// <param name="resource">The resource.</param>
        protected virtual void Initialize(Resource resource)
        {
            Resource = ToDispose(resource);
            if (resource != null)
            {
                resource.Tag = this;
            }
        }

        /// <summary>
        /// Implicit casting operator to <see cref="SharpDX.Direct3D11.Resource"/>
        /// </summary>
        /// <param name="from">The GraphicsResource to convert from.</param>
        public static implicit operator Resource(Direct2DResource from)
        {
            return from == null ? null : from.Resource;
        }

        protected override void Dispose(bool disposeManagedResources)
        {
            base.Dispose(disposeManagedResources);
            if (disposeManagedResources)
                Resource = null;
        }

        /// <summary>
        /// Called when name changed for this component.
        /// </summary>
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == "Name")
            {
                if (Device.IsDebugMode && Resource != null) Resource.Tag = Name;
            }
        }

        protected static void UnPin(GCHandle[] handles)
        {
            if (handles != null)
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].IsAllocated)
                        handles[i].Free();
                }
            }
        }

        public virtual void Initialize()
        {
            Initialize(Resource);
        }
    }
}

#endif