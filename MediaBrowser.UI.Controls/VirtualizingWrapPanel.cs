using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// http://www.codeproject.com/Articles/75847/Virtualizing-WrapPanel
    /// Positions child elements in sequential position from left to right, breaking content 
    /// to the next line at the edge of the containing box. Subsequent ordering happens 
    /// sequentially from top to bottom or from right to left, depending on the value of 
    /// the Orientation property.
    /// </summary>
    [DefaultProperty("Orientation")]
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        /// <summary>
        /// Identifies the ItemHeight dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register("ItemHeight", typeof(double), typeof(VirtualizingWrapPanel), new PropertyMetadata(100.0, new PropertyChangedCallback(VirtualizingWrapPanel.OnAppearancePropertyChanged)));
        /// <summary>
        /// Identifies the Orientation dependency property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(VirtualizingWrapPanel), new PropertyMetadata(Orientation.Horizontal, new PropertyChangedCallback(VirtualizingWrapPanel.OnAppearancePropertyChanged)));
        /// <summary>
        /// Identifies the ItemWidth dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register("ItemWidth", typeof(double), typeof(VirtualizingWrapPanel), new PropertyMetadata(100.0, new PropertyChangedCallback(VirtualizingWrapPanel.OnAppearancePropertyChanged)));
        /// <summary>
        /// Identifies the ScrollStep dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollStepProperty = DependencyProperty.Register("ScrollStep", typeof(double), typeof(VirtualizingWrapPanel), new PropertyMetadata(10.0, new PropertyChangedCallback(VirtualizingWrapPanel.OnAppearancePropertyChanged)));
        private bool canHorizontallyScroll;
        private bool canVerticallyScroll;
        private Size contentExtent = new Size(0.0, 0.0);
        private Point contentOffset = default(Point);
        private ScrollViewer scrollOwner;
        private Size viewport = new Size(0.0, 0.0);
        private int previousItemCount;
        /// <summary>
        /// Gets or sets a value that specifies the height of all items that are 
        /// contained within a VirtualizingWrapPanel. This is a dependency property.
        /// </summary>
        public double ItemHeight
        {
            get
            {
                return (double)base.GetValue(VirtualizingWrapPanel.ItemHeightProperty);
            }
            set
            {
                base.SetValue(VirtualizingWrapPanel.ItemHeightProperty, value);
            }
        }
        /// <summary>
        /// Gets or sets a value that specifies the width of all items that are 
        /// contained within a VirtualizingWrapPanel. This is a dependency property.
        /// </summary>
        public double ItemWidth
        {
            get
            {
                return (double)base.GetValue(VirtualizingWrapPanel.ItemWidthProperty);
            }
            set
            {
                base.SetValue(VirtualizingWrapPanel.ItemWidthProperty, value);
            }
        }
        /// <summary>
        /// Gets or sets a value that specifies the dimension in which child 
        /// content is arranged. This is a dependency property.
        /// </summary>
        public Orientation Orientation
        {
            get
            {
                return (Orientation)base.GetValue(VirtualizingWrapPanel.OrientationProperty);
            }
            set
            {
                base.SetValue(VirtualizingWrapPanel.OrientationProperty, value);
            }
        }
        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the horizontal axis is possible.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get
            {
                return this.canHorizontallyScroll;
            }
            set
            {
                if (this.canHorizontallyScroll != value)
                {
                    this.canHorizontallyScroll = value;
                    base.InvalidateMeasure();
                }
            }
        }
        /// <summary>
        /// Gets or sets a value that indicates whether scrolling on the vertical axis is possible.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get
            {
                return this.canVerticallyScroll;
            }
            set
            {
                if (this.canVerticallyScroll != value)
                {
                    this.canVerticallyScroll = value;
                    base.InvalidateMeasure();
                }
            }
        }
        /// <summary>
        /// Gets or sets a ScrollViewer element that controls scrolling behavior.
        /// </summary>
        public ScrollViewer ScrollOwner
        {
            get
            {
                return this.scrollOwner;
            }
            set
            {
                this.scrollOwner = value;
            }
        }
        /// <summary>
        /// Gets the vertical offset of the scrolled content.
        /// </summary>
        public double VerticalOffset
        {
            get
            {
                return this.contentOffset.Y;
            }
        }
        /// <summary>
        /// Gets the vertical size of the viewport for this content.
        /// </summary>
        public double ViewportHeight
        {
            get
            {
                return this.viewport.Height;
            }
        }
        /// <summary>
        /// Gets the horizontal size of the viewport for this content.
        /// </summary>
        public double ViewportWidth
        {
            get
            {
                return this.viewport.Width;
            }
        }
        /// <summary>
        /// Gets or sets a value for mouse wheel scroll step.
        /// </summary>
        public double ScrollStep
        {
            get
            {
                return (double)base.GetValue(VirtualizingWrapPanel.ScrollStepProperty);
            }
            set
            {
                base.SetValue(VirtualizingWrapPanel.ScrollStepProperty, value);
            }
        }
        /// <summary>
        /// Gets the vertical size of the extent.
        /// </summary>
        public double ExtentHeight
        {
            get
            {
                return this.contentExtent.Height;
            }
        }
        /// <summary>
        /// Gets the horizontal size of the extent.
        /// </summary>
        public double ExtentWidth
        {
            get
            {
                return this.contentExtent.Width;
            }
        }
        /// <summary>
        /// Gets the horizontal offset of the scrolled content.
        /// </summary>
        public double HorizontalOffset
        {
            get
            {
                return this.contentOffset.X;
            }
        }
        /// <summary>
        /// Scrolls down within content by one logical unit.
        /// </summary>
        public void LineDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + this.ScrollStep);
        }
        /// <summary>
        /// Scrolls left within content by one logical unit.
        /// </summary>
        public void LineLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - this.ScrollStep);
        }
        /// <summary>
        /// Scrolls right within content by one logical unit.
        /// </summary>
        public void LineRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + this.ScrollStep);
        }
        /// <summary>
        /// Scrolls up within content by one logical unit.
        /// </summary>
        public void LineUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - this.ScrollStep);
        }
        /// <summary>
        /// Forces content to scroll until the coordinate space of a Visual object is visible.
        /// </summary>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            this.MakeVisible(visual as UIElement);
            return rectangle;
        }
        /// <summary>
        /// Scrolls down within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + this.ScrollStep);
        }
        /// <summary>
        /// Scrolls left within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - this.ScrollStep);
        }
        /// <summary>
        /// Scrolls right within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + this.ScrollStep);
        }
        /// <summary>
        /// Scrolls up within content after a user clicks the wheel button on a mouse.
        /// </summary>
        public void MouseWheelUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - this.ScrollStep);
        }
        /// <summary>
        /// Scrolls down within content by one page.
        /// </summary>
        public void PageDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + this.ViewportHeight);
        }
        /// <summary>
        /// Scrolls left within content by one page.
        /// </summary>
        public void PageLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - this.ViewportHeight);
        }
        /// <summary>
        /// Scrolls right within content by one page.
        /// </summary>
        public void PageRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + this.ViewportHeight);
        }
        /// <summary>
        /// Scrolls up within content by one page.
        /// </summary>
        public void PageUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - this.viewport.Height);
        }
        /// <summary>
        /// Sets the amount of vertical offset.
        /// </summary>
        public void SetVerticalOffset(double offset)
        {
            if (offset < 0.0 || this.ViewportHeight >= this.ExtentHeight)
            {
                offset = 0.0;
            }
            else
            {
                if (offset + this.ViewportHeight >= this.ExtentHeight)
                {
                    offset = this.ExtentHeight - this.ViewportHeight;
                }
            }
            this.contentOffset.Y = offset;
            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }
            base.InvalidateMeasure();
        }
        /// <summary>
        /// Sets the amount of horizontal offset.
        /// </summary>
        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0.0 || this.ViewportWidth >= this.ExtentWidth)
            {
                offset = 0.0;
            }
            else
            {
                if (offset + this.ViewportWidth >= this.ExtentWidth)
                {
                    offset = this.ExtentWidth - this.ViewportWidth;
                }
            }
            this.contentOffset.X = offset;
            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }
            base.InvalidateMeasure();
        }
        /// <summary>
        /// Note: Works only for vertical.
        /// </summary>
        internal void PageLast()
        {
            this.contentOffset.Y = this.ExtentHeight;
            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }
            base.InvalidateMeasure();
        }
        /// <summary>
        /// Note: Works only for vertical.
        /// </summary>
        internal void PageFirst()
        {
            this.contentOffset.Y = 0.0;
            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }
            base.InvalidateMeasure();
        }
        /// <summary>
        /// When items are removed, remove the corresponding UI if necessary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    base.RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    return;
                case NotifyCollectionChangedAction.Reset:
                    {
                        ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
                        if (itemsControl != null)
                        {
                            if (this.previousItemCount != itemsControl.Items.Count)
                            {
                                if (this.Orientation == Orientation.Horizontal)
                                {
                                    this.SetVerticalOffset(0.0);
                                }
                                else
                                {
                                    this.SetHorizontalOffset(0.0);
                                }
                            }
                            this.previousItemCount = itemsControl.Items.Count;
                        }
                        return;
                    }
                default:
                    return;
            }
        }
        /// <summary>
        /// Measure the children.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            this.InvalidateScrollInfo(availableSize);
            int firstVisibleIndex;
            int lastVisibleIndex;
            if (this.Orientation == Orientation.Horizontal)
            {
                this.GetVerticalVisibleRange(out firstVisibleIndex, out lastVisibleIndex);
            }
            else
            {
                this.GetHorizontalVisibleRange(out firstVisibleIndex, out lastVisibleIndex);
            }
            UIElementCollection children = base.Children;
            IItemContainerGenerator generator = base.ItemContainerGenerator;
            if (generator != null)
            {
                GeneratorPosition startPos = generator.GeneratorPositionFromIndex(firstVisibleIndex);
                int childIndex = (startPos.Offset == 0) ? startPos.Index : (startPos.Index + 1);
                if (childIndex == -1)
                {
                    this.RefreshOffset();
                }
                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    int itemIndex = firstVisibleIndex;
                    while (itemIndex <= lastVisibleIndex)
                    {
                        bool newlyRealized;
                        UIElement child = generator.GenerateNext(out newlyRealized) as UIElement;
                        if (newlyRealized)
                        {
                            if (childIndex >= children.Count)
                            {
                                base.AddInternalChild(child);
                            }
                            else
                            {
                                base.InsertInternalChild(childIndex, child);
                            }
                            generator.PrepareItemContainer(child);
                        }
                        if (child != null)
                        {
                            child.Measure(new Size(this.ItemWidth, this.ItemHeight));
                        }
                        itemIndex++;
                        childIndex++;
                    }
                }
                this.CleanUpChildren(firstVisibleIndex, lastVisibleIndex);
            }
            if (IsCloseTo(availableSize.Height, double.PositiveInfinity) || IsCloseTo(availableSize.Width, double.PositiveInfinity))
            {
                return base.MeasureOverride(availableSize);
            }

            var itemsControl = ItemsControl.GetItemsOwner(this);
            var numItems = itemsControl.Items.Count;

            var width = availableSize.Width;
            var height = availableSize.Height;

            if (Orientation == Orientation.Vertical)
            {
                var numRows = Math.Floor(availableSize.Height / ItemHeight);

                height = numRows * ItemHeight;

                var requiredColumns = Math.Ceiling(numItems / numRows);

                width = Math.Min(requiredColumns * ItemWidth, width);
            }
            else
            {
                var numColumns = Math.Floor(availableSize.Width / ItemWidth);

                width = numColumns * ItemWidth;

                //if (numItems > 0 && numItems < numColumns)
                //{
                //    width = Math.Min(numColumns, numItems) * ItemWidth;
                //}

                var requiredRows = Math.Ceiling(numItems / numColumns);

                height = Math.Min(requiredRows * ItemHeight, height);
            }

            return new Size(width, height);
        }

        /// <summary>
        /// Arranges the children.
        /// </summary>
        /// <param name="finalSize">The available size.</param>
        /// <returns>The used size.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            bool isHorizontal = this.Orientation == Orientation.Horizontal;
            this.InvalidateScrollInfo(finalSize);
            int i = 0;
            foreach (object item in base.Children)
            {
                this.ArrangeChild(isHorizontal, finalSize, i++, item as UIElement);
            }
            return finalSize;
        }
        private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement panel = d as UIElement;
            if (panel != null)
            {
                panel.InvalidateMeasure();
            }
        }
        private void MakeVisible(UIElement element)
        {
            ItemContainerGenerator generator = base.ItemContainerGenerator.GetItemContainerGeneratorForPanel(this);
            if (element != null && generator != null)
            {
                for (int itemIndex = generator.IndexFromContainer(element); itemIndex == -1; itemIndex = generator.IndexFromContainer(element))
                {
                    element = element.ParentOfType<UIElement>();
                }
                ScrollViewer scrollViewer = element.ParentOfType<ScrollViewer>();
                if (scrollViewer != null)
                {
                    GeneralTransform elementTransform = element.TransformToVisual(scrollViewer);
                    Rect elementRectangle = elementTransform.TransformBounds(new Rect(new Point(0.0, 0.0), element.RenderSize));

                    if (this.Orientation == Orientation.Horizontal)
                    {
                        var padding = ItemHeight / 3;

                        if (elementRectangle.Bottom > this.ViewportHeight)
                        {
                            this.SetVerticalOffset(this.contentOffset.Y + elementRectangle.Bottom - this.ViewportHeight + padding);
                            return;
                        }
                        if (elementRectangle.Top < 0.0)
                        {
                            this.SetVerticalOffset(this.contentOffset.Y + elementRectangle.Top - padding);
                            return;
                        }
                    }
                    else
                    {
                        var padding = ItemWidth / 3;

                        if (elementRectangle.Right > this.ViewportWidth)
                        {
                            this.SetHorizontalOffset(this.contentOffset.X + elementRectangle.Right - this.ViewportWidth + padding);
                            return;
                        }
                        if (elementRectangle.Left < 0.0)
                        {
                            this.SetHorizontalOffset(this.contentOffset.X + elementRectangle.Left - padding);
                        }
                    }
                }
            }
        }
        private void GetVerticalVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
        {
            int childrenPerRow = this.GetVerticalChildrenCountPerRow(this.contentExtent);
            firstVisibleItemIndex = (int)Math.Floor(this.VerticalOffset / this.ItemHeight) * childrenPerRow;
            lastVisibleItemIndex = (int)Math.Ceiling((this.VerticalOffset + this.ViewportHeight) / this.ItemHeight) * childrenPerRow - 1;
            this.AdjustVisibleRange(ref firstVisibleItemIndex, ref lastVisibleItemIndex);
        }
        private void GetHorizontalVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
        {
            int childrenPerRow = this.GetHorizontalChildrenCountPerRow(this.contentExtent);
            firstVisibleItemIndex = (int)Math.Floor(this.HorizontalOffset / this.ItemWidth) * childrenPerRow;
            lastVisibleItemIndex = (int)Math.Ceiling((this.HorizontalOffset + this.ViewportWidth) / this.ItemWidth) * childrenPerRow - 1;
            this.AdjustVisibleRange(ref firstVisibleItemIndex, ref lastVisibleItemIndex);
        }
        private void AdjustVisibleRange(ref int firstVisibleItemIndex, ref int lastVisibleItemIndex)
        {
            firstVisibleItemIndex--;
            lastVisibleItemIndex++;
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            if (itemsControl != null)
            {
                if (firstVisibleItemIndex < 0)
                {
                    firstVisibleItemIndex = 0;
                }
                if (lastVisibleItemIndex >= itemsControl.Items.Count)
                {
                    lastVisibleItemIndex = itemsControl.Items.Count - 1;
                }
            }
        }
        private void CleanUpChildren(int minIndex, int maxIndex)
        {
            UIElementCollection children = base.Children;
            IItemContainerGenerator generator = base.ItemContainerGenerator;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                GeneratorPosition pos = new GeneratorPosition(i, 0);
                int itemIndex = generator.IndexFromGeneratorPosition(pos);
                if (itemIndex < minIndex || itemIndex > maxIndex)
                {
                    generator.Remove(pos, 1);
                    base.RemoveInternalChildRange(i, 1);
                }
            }
        }
        private void ArrangeChild(bool isHorizontal, Size finalSize, int index, UIElement child)
        {
            if (child == null)
            {
                return;
            }
            int count = isHorizontal ? this.GetVerticalChildrenCountPerRow(finalSize) : this.GetHorizontalChildrenCountPerRow(finalSize);
            int itemIndex = base.ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(index, 0));
            int row = isHorizontal ? (itemIndex / count) : (itemIndex % count);
            int column = isHorizontal ? (itemIndex % count) : (itemIndex / count);
            Rect rect = new Rect((double)column * this.ItemWidth, (double)row * this.ItemHeight, this.ItemWidth, this.ItemHeight);
            if (isHorizontal)
            {
                rect.Y -= this.VerticalOffset;
            }
            else
            {
                rect.X -= this.HorizontalOffset;
            }
            child.Arrange(rect);
        }
        private void InvalidateScrollInfo(Size availableSize)
        {
            ItemsControl ownerItemsControl = ItemsControl.GetItemsOwner(this);
            if (ownerItemsControl != null)
            {
                Size extent = this.GetExtent(availableSize, ownerItemsControl.Items.Count);
                if (extent != this.contentExtent)
                {
                    this.contentExtent = extent;
                    this.RefreshOffset();
                }
                if (availableSize != this.viewport)
                {
                    this.viewport = availableSize;
                    this.InvalidateScrollOwner();
                }
            }
        }
        private void RefreshOffset()
        {
            if (this.Orientation == Orientation.Horizontal)
            {
                this.SetVerticalOffset(this.VerticalOffset);
                return;
            }
            this.SetHorizontalOffset(this.HorizontalOffset);
        }
        private void InvalidateScrollOwner()
        {
            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }
        }
        private Size GetExtent(Size availableSize, int itemCount)
        {
            if (this.Orientation == Orientation.Horizontal)
            {
                int childrenPerRow = this.GetVerticalChildrenCountPerRow(availableSize);
                return new Size((double)childrenPerRow * this.ItemWidth, this.ItemHeight * Math.Ceiling((double)itemCount / (double)childrenPerRow));
            }
            int childrenPerRow2 = this.GetHorizontalChildrenCountPerRow(availableSize);
            return new Size(this.ItemWidth * Math.Ceiling((double)itemCount / (double)childrenPerRow2), (double)childrenPerRow2 * this.ItemHeight);
        }
        private int GetVerticalChildrenCountPerRow(Size availableSize)
        {
            int childrenCountPerRow;
            if (availableSize.Width == double.PositiveInfinity)
            {
                childrenCountPerRow = base.Children.Count;
            }
            else
            {
                childrenCountPerRow = Math.Max(1, (int)Math.Floor(availableSize.Width / this.ItemWidth));
            }
            return childrenCountPerRow;
        }
        private int GetHorizontalChildrenCountPerRow(Size availableSize)
        {
            int childrenCountPerRow;
            if (availableSize.Height == double.PositiveInfinity)
            {
                childrenCountPerRow = base.Children.Count;
            }
            else
            {
                childrenCountPerRow = Math.Max(1, (int)Math.Floor(availableSize.Height / this.ItemHeight));
            }
            return childrenCountPerRow;
        }

        private static bool IsCloseTo(double value1, double value2)
        {
            return AreClose(value1, value2);
        }

        private static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
            {
                return true;
            }
            double num = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.2204460492503131E-16;
            double num2 = value1 - value2;
            return -num < num2 && num > num2;
        }
    }
}