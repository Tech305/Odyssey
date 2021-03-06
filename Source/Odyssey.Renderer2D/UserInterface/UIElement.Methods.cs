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
using System.Collections.Generic;
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
using Odyssey.UserInterface.Events;
using Odyssey.UserInterface.Serialization;
using Odyssey.UserInterface.Style;
using SharpDX.Mathematics;
#endregion

namespace Odyssey.UserInterface
{
    public abstract partial class UIElement: IEnumerable<UIElement>
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

        internal TElement FindAncestor<TElement>(Func<TElement, bool> f)
            where TElement:UIElement
        {
            var ancestor = (TElement)parent;
            while (ancestor != null)
            {
                if (f(ancestor))
                    return ancestor;
                ancestor = (TElement)ancestor.Parent;
            }
            return null;
        }

        internal IEnumerable<TElement> FindDescendants<TElement>(Func<TElement, bool> f)
            where TElement : UIElement
        {
            return TreeTraversal.PreOrderVisit(this).OfType<TElement>().Where(f);
        }

        public TElement FindAncestor<TElement>() where TElement : UIElement
        {
            UIElement ancestor = parent;
            while (ancestor != null)
            {
                var tAncestor = ancestor as TElement;
                if (tAncestor != null)
                    return tAncestor;
                ancestor = ancestor.Parent;
            }
            return null;
        }

        public IEnumerable<TElement> FindDescendants<TElement>() where TElement : UIElement
        {
            return TreeTraversal.PreOrderVisit(this).OfType<TElement>();
        }

        #region Layout

        private void ForceMeasure()
        {
            IsMeasureValid = false;
            if (Parent != null)
            {
                Parent.ForceMeasure();
                Measure(Parent.RenderSize + Parent.MarginInternal);
            }
            InvalidateArrange();
        }

        private void ForceArrange()
        {
            IsArrangeValid = false;
            if (Parent != null)
            {
                Parent.ForceArrange();
                Arrange(Parent.DesiredSizeWithMargins);
            }
        }

        protected void InvalidateMeasure()
        {
            ForceMeasure();
            PropagateMeasureInvalidationToChildren();
        }

        protected void InvalidateArrange()
        {
            ForceArrange();
            PropagateArrangeInvalidationToChildren();
        }


        private void PropagateMeasureInvalidationToChildren()
        {
            foreach (var child in this)
            {
                child.IsMeasureValid = false;
                child.PropagateMeasureInvalidationToChildren();
            }
        }

        private void PropagateArrangeInvalidationToChildren()
        {
            foreach (var child in this)
            {
                child.IsArrangeValid = false;
                child.PropagateArrangeInvalidationToChildren();
            }
        }

        public void Arrange(Vector3 availableSizeWithMargins)
        {
            if (IsArrangeValid)
                return;
            IsArrangeValid = true;

            Vector3 oldAbsolutePosition = absolutePosition;
            PositionOffsets += new Vector3(Margin.Left, Margin.Top, 0);
            var newAbsolutePosition = Position + PositionOffsets;
            if (parent != null)
                newAbsolutePosition += parent.AbsolutePosition;

            if (!newAbsolutePosition.Equals(oldAbsolutePosition))
                AbsolutePosition = newAbsolutePosition;

            var elementSize = Size;
            var finalSizeWithoutMargins = availableSizeWithMargins - MarginInternal;
            if (float.IsNaN(elementSize.X) && HorizontalAlignment == HorizontalAlignment.Stretch)
                elementSize.X = finalSizeWithoutMargins.X;
            if (float.IsNaN(elementSize.Y) && VerticalAlignment == VerticalAlignment.Stretch)
                elementSize.Y = finalSizeWithoutMargins.Y;
            if (float.IsNaN(elementSize.Z))
                elementSize.Z = finalSizeWithoutMargins.Z;

            if (float.IsNaN(elementSize.X))
                elementSize.X = Math.Min(DesiredSize.X, finalSizeWithoutMargins.X);
            if (float.IsNaN(elementSize.Y))
                elementSize.Y = Math.Min(DesiredSize.Y, finalSizeWithoutMargins.Y);
            if (float.IsNaN(elementSize.Y))
                elementSize.Z = Math.Min(DesiredSize.Z, finalSizeWithoutMargins.Z);

            // trunk the element size between the maximum and minimum width/height of the UIElement
            elementSize = new Vector3(
                Math.Max(MinimumWidth, Math.Min(MaximumWidth, elementSize.X)),
                Math.Max(MinimumHeight, Math.Min(MaximumHeight, elementSize.Y)),
                Math.Max(MinimumDepth, Math.Min(MaximumDepth, elementSize.Z)));

            elementSize = ArrangeOverride(elementSize);
            RenderSize = elementSize;

            PositionOffsets = CalculatePosition(availableSizeWithMargins, elementSize);
            if (PositionOffsets != Vector3.Zero)
            {
                AbsolutePosition += PositionOffsets;
                foreach (var element in Children)
                    element.PositionOffsets += PositionOffsets;
            }
        }

        public void Measure(Vector3 availableSize)
        {
            if (IsMeasureValid)
                return;

            IsMeasureValid = true;
            var desiredSize = new Vector3(Width, Height, Depth);

            if (float.IsNaN(desiredSize.X) || float.IsNaN(desiredSize.Y) || float.IsNaN(desiredSize.Z))
            {
                var availableSizeWithoutMargins = availableSize - MarginInternal;
                availableSizeWithoutMargins = new Vector3(
                    Math.Max(MinimumWidth, Math.Min(MaximumWidth, !float.IsNaN(desiredSize.X) ? desiredSize.X : availableSizeWithoutMargins.X)),
                    Math.Max(MinimumHeight, Math.Min(MaximumHeight, !float.IsNaN(desiredSize.Y) ? desiredSize.Y : availableSizeWithoutMargins.Y)),
                    Math.Max(MinimumDepth, Math.Min(MaximumDepth, !float.IsNaN(desiredSize.Z) ? desiredSize.Z : availableSizeWithoutMargins.Z)));

                var childrenDesiredSize = MeasureOverride(availableSizeWithoutMargins);
                if (float.IsNaN(desiredSize.X))
                    desiredSize.X = childrenDesiredSize.X;
                if (float.IsNaN(desiredSize.Y))
                    desiredSize.Y = childrenDesiredSize.Y;
                if (float.IsNaN(desiredSize.Z))
                    desiredSize.Z = childrenDesiredSize.Z;
            }

            desiredSize = new Vector3(
                    Math.Max(MinimumWidth, Math.Min(MaximumWidth, desiredSize.X)),
                    Math.Max(MinimumHeight, Math.Min(MaximumHeight, desiredSize.Y)),
                    Math.Max(MinimumDepth, Math.Min(MaximumDepth, desiredSize.Z)));

            DesiredSize = desiredSize;
            DesiredSizeWithMargins = desiredSize + MarginInternal;
        }

        private Vector3 CalculatePosition(Vector3 availableSpace, Vector3 usedSpace)
        {
            Vector3 offsets = Vector3.Zero;
            switch (VerticalAlignment)
            {
                case VerticalAlignment.Bottom:
                    offsets.Y += availableSpace.Y - usedSpace.Y;

                    break;

                case VerticalAlignment.Center:
                    offsets.Y += (availableSpace.Y - usedSpace.Y) / 2;
                    break;
            }

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    offsets.X += (availableSpace.X - usedSpace.X) / 2;
                    break;

                case HorizontalAlignment.Right:
                    offsets.X += availableSpace.X - usedSpace.X;
                    break;
            }
            return offsets;
        }

        internal void SetPosition(Vector3 newPosition)
        {
            position = newPosition;
        }

        internal void SetSize(int dimension, float value)
        {
            if (dimension == 0)
                width = value;
            else if (dimension == 1)
                height = value;
            else if (dimension == 2)
                depth = value;
            else
                throw new ArgumentOutOfRangeException("dimension");
        }

        public void BringToFront()
        {
            float zIndex = Parent.Children.Max(e => e.Position.Z);
            Position = new Vector3(position.X, position.Y, ++zIndex);
            Parent.Children.Sort(e=> e.Position.Z);
        }

        public void Layout(Vector3 availableSize)
        {
            Measure(availableSize);
            Arrange(DesiredSizeWithMargins);
        }

        protected virtual Vector3 ArrangeOverride(Vector3 availableSizeWithoutMargins)
        {
            return availableSizeWithoutMargins;
        }

        protected virtual Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            return Vector3.Zero;
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

            foreach (UIElement element in Children)
                element.Initialize();

            OnInitialized(args);
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
            newElement.Margin = Margin;
            newElement.horizontalAlignment = horizontalAlignment;
            newElement.verticalAlignment = verticalAlignment;

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

        public static Vector3 ScreenToLocalCoordinates(UIElement element, Vector2 screenCoordinates)
        {
            Vector3 offset = new Vector3(screenCoordinates,0) - element.AbsolutePosition;
            return element.Position + offset;
        }

        public IEnumerator<UIElement> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}