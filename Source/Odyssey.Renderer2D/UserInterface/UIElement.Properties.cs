#region #Disclaimer

// /*
// * Timer
// *
// * Created on 21 August 2007
// * Last update on 29 July 2010
// *
// * Author: Adalberto L. Simeone (Taranto, Italy)
// * E-Mail: avengerdragon@gmail.com
// * Website: http://www.avengersutd.com
// *
// * Part of the Odyssey Engine.
// *
// * This source code is Intellectual property of the Author
// * and is released under the Creative Commons Attribution
// * NonCommercial License, available at:
// * http://creativecommons.org/licenses/by-nc/3.0/
// * You can alter and use this source code as you wish,
// * provided that you do not use the results in commercial
// * projects, without the express and written consent of
// * the Author.
// *
// * /

#endregion #Disclaimer

#region Using Directives

using Odyssey.Engine;
using Odyssey.UserInterface.Controls;
using Odyssey.UserInterface.Data;
using Odyssey.UserInterface.Style;
using SharpDX;
using System;
using System.Collections.Generic;

#endregion Using Directives

namespace Odyssey.UserInterface
{
    public abstract partial class UIElement
    {
        private float height;
        private float width;

        #region Public properties

        public RectangleF BoundingRectangle
        {
            get { return boundingRectangle; }
            //set
            //{
            //    RectangleF oldValue = boundingRectangle;
            //    if (boundingRectangle != value)
            //        boundingRectangle = value;
            //    else
            //        return;

            //    if (oldValue.X != boundingRectangle.X || oldValue.Y != boundingRectangle.Y)
            //        OnPositionChanged(EventArgs.Empty);

            //    if (oldValue.Width != boundingRectangle.Width || oldValue.Height != boundingRectangle.Height)
            //        OnSizeChanged(EventArgs.Empty);
            //}
        }

        public object DataContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the control is in design mode.
        /// </summary>
        /// <value><c>true</c> if the control is in design mode; otherwise, /c>.</value>
        /// <remarks>
        /// While in design mode, certain events are not fired.
        /// </remarks>
        public virtual bool DesignMode
        {
            get { return designMode; }
            protected internal set
            {
                if (designMode != value)
                {
                    designMode = value;
                    OnDesignModeChanged(new ControlEventArgs(this));

                    IContainer container = this as IContainer;
                    if (container != null)
                    {
                        foreach (UIElement childControl in container.Controls)
                            childControl.DesignMode = value;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether this control has captured the mouse pointer.
        /// </summary>
        /// <value><c>true</c> if the control has captured the mouse cursor, <c>false</c>
        /// otherwise.</value>
        /// <remarks>
        /// When a control captures the mouse pointer, events are only sent to that control.
        /// </remarks>
        public bool HasCaptured { get; internal set; }

        public float Height
        {
            get { return height; }
            set
            {
                if (height == value)
                    return;

                height = value;

                if (DesignMode) return;

                OnSizeChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets the zero based index of this control in the <see cref = "ContainerControl" />.
        /// </summary>
        /// <value>The zero based index.</value>
        public int Index { get; internal set; }

        /// <summary>
        /// Determines whether this control can be focused.
        /// </summary>
        /// <value><c>true</b> if the control can be focused; <c>false</c> otherwise.</value>
        public bool IsFocusable
        {
            get { return isFocusable; }
            internal set { isFocusable = value; }
        }

        public Thickness Margin { get; set; }

        /// <summary>
        /// Gets or sets the height and width of the control.
        /// </summary>
        /// <value>The <see cref = "SharpDX.Size2">Size</see> that represents the height and
        /// width of the control in pixels.</value>
        public Size2F Size
        {
            get { return new Size2F(Width, Height); }
        }

        public float Width
        {
            get { return width; }
            set
            {
                if (width == value)
                    return;

                width = value;

                if (DesignMode) return;
                OnSizeChanged(EventArgs.Empty);
            }
        }

        internal Vector3 AbsoluteOrthoPosition { get; set; }

        internal IEnumerable<BindingExpression> Bindings
        {
            get { return bindings.Values; }
        }

        internal IServiceRegistry Services { get; set; }

        /// <summary>
        /// Returns true if the control is being updated (ie, it is in the updateQueue collection),
        /// false otherwise.
        /// </summary>
        /// <value><c>true</c> if this instance is being updated; otherwise, <c>false</c>.</value>
        protected internal bool IsBeingUpdated { get; set; }

        /// <summary>
        /// Gets the top left position in the client area of the control.
        /// </summary>
        /// <value>The top left position.</value>
        /// <remarks>
        /// The top left position is computed considering the <see cref = "BorderSize" /> value and
        /// the <see cref = "Thickness" />value.
        /// </remarks>
        protected internal Vector2 TopLeftPosition { get; set; }

        /// <summary>
        /// Gets the absolute position in screen coordinates of the upper-left corner of this
        /// control.
        /// </summary>
        /// <value>A <see cref = "Microsoft.DirectX.Vector2" /> that represents the absolute
        /// position of the upper-left corner in screen coordinates for this control.</value>
        public Vector2 AbsolutePosition { get; internal set; }

        /// <summary>
        /// Gets or sets a value that will be used by the interface to know whether that control can
        /// raise events or not.
        /// </summary>
        /// <value><c>true</c> if the control can react to events; <c>false</c> otherwise.</value>
        /// <remarks>
        /// Setting this property to <b>false</b> will lock the control in its current state.
        /// </remarks>
        public bool CanRaiseEvents
        {
            get { return canRaiseEvents; }
            set { canRaiseEvents = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the control can be interacted with.
        /// </summary>
        /// <remarks>
        /// This consequently causes the <see cref = "UIElement.CanRaiseEvents" /> property to be
        /// set.
        /// </remarks>
        public bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = canRaiseEvents = value; }
        }

        /// <summary>
        /// Determines whether this control is focused.
        /// </summary>
        /// <value><c>true</c> if this control is focused; <c>false</c> otherwise.</value>
        public bool IsFocused { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this control is highlighted.
        /// </summary>
        /// <value><c>true</c> if this control is highlighted; otherwise, <c>false</c>.</value>
        public bool IsHighlighted
        {
            get { return isHighlighted; }
            set
            {
                if (isHighlighted != value)
                {
                    isHighlighted = value;
                    OnHighlightedChanged(EventArgs.Empty);
                }
            }
        }

        public bool IsInited { get; protected set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the control is visible or not.
        /// </summary>
        /// <value><c>true</c> if the control is visible; <c>false</c> otherwise.</value>
        /// <remarks>
        /// Setting this property to a different value that the one it had before the assignment,
        /// will cause the UI to be recomputed if the control is not in <see cref = "DesignMode" />
        /// </remarks>
        public virtual bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (value != isVisible)
                {
                    isVisible = value;
                    OnVisibleChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets or Sets the parent control. When a new parent control is set the absolute position
        /// of the child control is also computed.
        /// </summary>
        /// <value>The parent control.</value>
        public virtual UIElement Parent
        {
            get { return parent; }
            internal set
            {
                if (parent != value)
                {
                    parent = value;
                    OnParentChanged(EventArgs.Empty);
                    IContainer formerParent = parent as IContainer;
                    if (formerParent != null)
                        formerParent.Controls.Remove(value);
                    Depth = Depth.AsChildOf(parent.Depth);
                }

                bool isOverlay = parent is OverlayBase;

                // Find the overlay we are in;
                if (isOverlay)
                    Overlay = (OverlayBase) Parent;
                else if (Parent.Overlay != null)
                    Overlay = Parent.Overlay;
            }
        }

        /// <summary>
        /// Gets or sets the coordinates of the upper-left corner of the control relative to the
        /// upper-left corner of its container.
        /// </summary>
        /// <value>A Vector2 that represents the upper-left corner of the control relative to the
        /// upper-left corner of its container.</value>
        /// <remarks>
        /// If the controls's <see cref = "Parent" /> is the <see cref = "OverlayBase" />, the
        /// <b>PositionV3</b> property value represents the upper-left corner of the control in
        /// screen coordinates.
        /// </remarks>
        public Vector2 Position
        {
            get { return position; }
            set
            {
                if (position == value) return;

                position = value;

                if (DesignMode) return;

                OnPositionChanged(EventArgs.Empty);
                OnMove(EventArgs.Empty);
            }
        }

        #endregion Public properties

        internal bool IsBeingRemoved { get; set; }

        internal OverlayBase Overlay { get; set; }

        protected Direct2DDevice Device
        {
            get { return Overlay.Device; }
        }

        #region Protected properties

        protected internal virtual Depth Depth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mouse cursor is currently inside.
        /// </summary>
        /// <value><c>true</c> if the mouse pointer is currently inside; otherwise,
        /// <c>false</c>.</value>
        protected internal bool IsInside
        {
            get { return isInside && canRaiseEvents; }
            set { isInside = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this control is clicked.
        /// </summary>
        /// <value><c>true</c> if this control is clicked; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This value stays <c>true</c> for as long as the user presses the mouse button.
        /// </remarks>
        protected internal bool IsPressed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this control is selected.
        /// </summary>
        /// <value><c>true</c> if this control is selected; otherwise, <c>false</c>.</value>
        protected internal bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnSelectedChanged(EventArgs.Empty);
            }
        }

        #endregion Protected properties
    }
}