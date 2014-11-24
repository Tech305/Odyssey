#region License

// Copyright � 2013-2014 Avengers UTD - Adalberto L. Simeone
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
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Xml;
using Odyssey.Content;
using Odyssey.Engine;
using Odyssey.Interaction;
using Odyssey.Logging;
using Odyssey.Reflection;
using Odyssey.UserInterface.Data;
using Odyssey.UserInterface.Serialization;
using Odyssey.UserInterface.Style;
using SharpDX.Mathematics;
#endregion

namespace Odyssey.UserInterface
{
    public abstract partial class UIElement
    {
        public void DeserializeXml(IResourceProvider theme, XmlReader xmlReader)
        {
            OnReadXml(new XmlDeserializationEventArgs(theme, xmlReader));
        }

        public void SerializeXml(IResourceProvider theme, XmlWriter xmlWriter)
        {
            OnWriteXml(new XmlSerializationEventArgs(theme, xmlWriter));
        }

        /// <summary>
        /// Programmatically focuses this <see cref="UIElement"/> object, <b>if</b> it is focusable.
        /// </summary>
        public void Focus()
        {
            if (isFocusable)
                OnGotFocus(EventArgs.Empty);
        }

        #region Layout

        public void Arrange(Vector2 availableSizeWithMargins)
        {
            Vector2 oldAbsolutePosition = AbsolutePosition;
            Vector2 newAbsolutePosition = new Vector2(Margin.Left, Margin.Top) + position;
            if (parent != null)
            {
                newAbsolutePosition += parent.AbsolutePosition;
                if (!IsInternal)
                    newAbsolutePosition += parent.TopLeftPosition;
            }

            if (!newAbsolutePosition.Equals(oldAbsolutePosition))
                AbsolutePosition = newAbsolutePosition;

            var elementSize = new Vector2(Width, Height);
            var finalSizeWithoutMargins = availableSizeWithMargins - new Vector2(Margin.Horizontal, Margin.Vertical);
            if (float.IsNaN(elementSize.X))
                elementSize.X = finalSizeWithoutMargins.X;
            if (float.IsNaN(elementSize.Y))
                elementSize.Y = finalSizeWithoutMargins.Y;

            if (float.IsNaN(elementSize.X))
                elementSize.X = Math.Min(DesiredSize.X, finalSizeWithoutMargins.X);
            if (float.IsNaN(elementSize.Y))
                elementSize.Y = Math.Min(DesiredSize.Y, finalSizeWithoutMargins.Y);

            // trunk the element size between the maximum and minimum width/height of the UIElement
            elementSize = new Vector2(
                Math.Max(MinimumWidth, Math.Min(MaximumWidth, elementSize.X)),
                Math.Max(MinimumHeight, Math.Min(MaximumHeight, elementSize.Y)));

            elementSize = ArrangeOverride(elementSize);
            RenderSize = elementSize;
        }

        public void Measure(Vector2 availableSize)
        {
            var desiredSize = new Vector2(Width, Height);

            if (float.IsNaN(desiredSize.X) || float.IsNaN(desiredSize.Y))
            {
                Vector2 availableSizeWithoutMargins = availableSize - new Vector2(Margin.Horizontal, Margin.Vertical);
                availableSizeWithoutMargins = new Vector2(
                    Math.Max(MinimumWidth, Math.Min(MaximumWidth, !float.IsNaN(desiredSize.X) ? desiredSize.X : availableSizeWithoutMargins.X)),
                    Math.Max(MinimumHeight, Math.Min(MaximumHeight, !float.IsNaN(desiredSize.Y) ? desiredSize.Y : availableSizeWithoutMargins.Y)));

                var childrenDesiredSize = MeasureOverride(availableSizeWithoutMargins);
                if (float.IsNaN(desiredSize.X))
                    desiredSize.X = childrenDesiredSize.X;
                if (float.IsNaN(desiredSize.Y))
                    desiredSize.Y = childrenDesiredSize.Y;
            }

            desiredSize = new Vector2(
                    Math.Max(MinimumWidth, Math.Min(MaximumWidth, desiredSize.X)),
                    Math.Max(MinimumHeight, Math.Min(MaximumHeight, desiredSize.Y)));

            DesiredSize = desiredSize;
            DesiredSizeWithMargins = desiredSize + new Vector2(Margin.Horizontal, Margin.Vertical);
        }

        public virtual void Layout(Vector2 availableSize)
        {
            Measure(availableSize);
            Arrange(DesiredSizeWithMargins);
        }

        protected virtual Vector2 ArrangeOverride(Vector2 availableSizeWithoutMargins)
        {
            return availableSizeWithoutMargins;
        }

        protected virtual Vector2 MeasureOverride(Vector2 availableSizeWithoutMargins)
        {
            return Vector2.Zero;
        } 

        #endregion

        public static explicit operator RectangleF(UIElement uiElement)
        {
            float x = uiElement.AbsolutePosition.X;
            float y = uiElement.AbsolutePosition.Y;
            float width = uiElement.Width;
            float height = uiElement.Height;

            return new RectangleF(x, y, width, height);
        }

        public void BringToFront()
        {
            Depth = new Depth(Depth.WindowLayer, Depth.ComponentLayer, Depth.Foreground);
        }

        public void Initialize()
        {
            var args = new EventArgs();
            
            foreach (var kvp in bindings)
            {
                var bindingExpression = kvp.Value;
                bindingExpression.SourceBinding.Source = DataContext;
                bindingExpression.Initialize();
            }

            OnInitializing(args);
            behaviors.Attach(this);

            if (Animator.HasAnimations)
                Animator.Initialize();

            OnInitialized(args);
        }

        ///// <summary>
        ///// Returns the window that this control belongs to, if any.
        ///// </summary>
        ///// <returns>The <see cref="Window"/> reference the control belongs to; <c>null</c> if the control doesn't
        ///// belong to any window.</returns>
        //public Window FindWindow()
        //{
        //    if (depth.WindowLayer == 0)
        //        return null;
        //    else
        //        return UserInterfaceManager.CurrentOverlay.WindowManager[depth.WindowLayer - 1];
        //}

        public void SendToBack()
        {
            Depth = new Depth(Depth.WindowLayer, Depth.ComponentLayer, Depth.Background);
        }

        public void SetBinding(Binding binding, string targetProperty)
        {
            Contract.Requires<ArgumentNullException>(binding != null, "binding");

            var bindingExpression = new BindingExpression(binding, this, targetProperty);

            bindings.Add(targetProperty, bindingExpression);
        }

        /// <summary>
        /// Creates a shallow copy of this object and its children.
        /// </summary>
        /// <returns>A new copy of this element.</returns>
        protected internal virtual UIElement Copy()
        {
            var newElement = (UIElement) Activator.CreateInstance(GetType());
            newElement.Name = Name ?? newElement.Name;
            newElement.Width = Width;
            newElement.Height = Height;
            newElement.Margin = Margin;

            CopyEvents(typeof(UIElement), this, newElement);           

            newElement.Animator.AddAnimations(Animator.Animations);
            return newElement;
        }

        protected static void CopyEvents(Type type, object source, object target)
        {
            var events = from f in ReflectionHelper.GetFields(type)
                         where f.FieldType.GetTypeInfo().BaseType == typeof(MulticastDelegate)
                         select f;

            foreach (var eventField in events)
            {
                var eventHandler = eventField.GetValue(source);
                if (eventHandler == null)
                    continue;
                eventField.SetValue(target, eventHandler);
            }
        }

        internal virtual bool ProcessKeyDown(KeyEventArgs e)
        {
            if (!CanRaiseEvents && KeyDown == null)
                return false;
            OnKeyDown(e);
            return true;
        }

        internal virtual bool ProcessKeyUp(KeyEventArgs e)
        {
            if (!CanRaiseEvents && KeyUp == null)
                return false;
            OnKeyUp(e);
            return true;
        }

        internal virtual bool ProcessPointerMovement(PointerEventArgs e)
        {
            Vector2 location = e.CurrentPoint.Position;
            if (!IsPointerCaptured && !Contains(location))
                return false;

            if (canRaiseEvents)
            {
                if (!isInside)
                    OnPointerEnter(e);

                OnPointerMoved(e);
                return true;
            }
            return false;
        }

        internal virtual bool ProcessPointerPressed(PointerEventArgs e)
        {
            Vector2 location = e.CurrentPoint.Position;
            if (!canRaiseEvents || !Contains(location))
                return false;

            if (!IsPressed && isEnabled)
            {
                if (e.CurrentPoint.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                    IsPressed = true;
                OnPointerPressed(e);

                if (isFocusable && !IsFocused)
                    OnGotFocus(EventArgs.Empty);
            }

            return true;
        }

        internal virtual bool ProcessPointerRelease(PointerEventArgs e)
        {
            Vector2 location = e.CurrentPoint.Position;
            if (canRaiseEvents && (IsPointerCaptured || Contains(location)))
            {
                if (IsPressed && e.CurrentPoint.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    OnTap(e);
                    IsPressed = false;
                }
                OnPointerReleased(e);
                return true;
            }

            if (IsPressed)
                IsPressed = false;
            return false;
        }

        public virtual void Update(ITimeService time)
        {
            if (Animator.HasAnimations && Animator.IsPlaying)
                Animator.Update(time);

            OnTick(new TimeEventArgs(time));
        }

        public bool CapturePointer()
        {
            if (CanRaiseEvents && IsEnabled)
            {
                Overlay.CaptureElement = this;
                return true;
            }
            return false;
        }

        public void ReleaseCapture()
        {
            Overlay.CaptureElement = null;
        }

        public static Vector2 ScreenToLocalCoordinates(UIElement element, Vector2 screenCoordinates)
        {
            Vector2 offset =screenCoordinates - element.AbsolutePosition;
            return element.Position + offset;
        }
    }
}