﻿#region Using Directives

using Odyssey.UserInterface.Data;
using Odyssey.Utilities.Logging;
using SharpDX;
using System.Collections;
using System.Linq;

#endregion Using Directives

namespace Odyssey.UserInterface.Controls
{
    public abstract class ItemsControl : Control, IContainer
    {
        private IEnumerable itemsSource;

        protected ItemsControl(string styleClass)
            : base(styleClass)
        {
            Children = new ControlCollection(this);
        }

        public DataTemplate DataTemplate { get; set; }

        public IEnumerable ItemsSource
        {
            get { return itemsSource; }
            set
            {
                if (itemsSource != value)
                {
                    itemsSource = value;
                    DataContext = value;
                }
            }
        }

        protected ControlCollection Children { get; set; }

        protected override void OnInitializing(ControlEventArgs e)
        {
            base.OnInitializing(e);
            if (DataTemplate == null)
                return;
            if (DataTemplate.DataType != GetType())
            {
                LogEvent.UserInterface.Warning(string.Format("DataTemplate '{0}' expects type {1}. Element '{2}' is of type {3}",
                    DataTemplate.Key, DataTemplate.DataType.Name, Name, GetType().Name));
                return;
            }

            Vector2 position = TopLeftPosition;
            foreach (var item in ItemsSource)
            {
                UIElement previousElement = this;
                foreach (UIElement element in TreeTraversal.PreOrderVisit(DataTemplate.VisualTree))
                {
                    UIElement newItem = element.Copy();
                    var contentControlParent = element.Parent as ContentControl;
                    if (contentControlParent != null)
                        ((ContentControl)previousElement).Content = newItem;
                    else
                        Children.Add(ToDispose(newItem));
                    newItem.DataContext = item;
                    foreach (var kvp in DataTemplate.Bindings.Where(kvp => newItem.Name == kvp.Value.TargetElement))
                    {
                        newItem.SetBinding(kvp.Value, kvp.Key);
                    }
                    newItem.Initialize();
                    previousElement = newItem;
                }

                //TODO Implement Binding
            }
        }

        #region IContainer members

        public ControlCollection Controls
        {
            get { return Children; }
        }

        void IContainer.Arrange()
        {
            Arrange();
        }

        public override void Render()
        {
            foreach (UIElement control in Controls.Where(control => control.IsVisible))
                control.Render();
        }

        protected internal override void Layout()
        {
            base.Layout();
            foreach (UIElement uiElement in Children)
                uiElement.Layout();

            Arrange();
        }

        protected abstract void Arrange();

        #endregion IContainer members
    }
}