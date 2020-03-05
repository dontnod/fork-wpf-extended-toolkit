using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Utils.Wpf.DragDrop;
using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;

namespace Xceed.Wpf.DataGrid
{

    [TemplatePart(Name = "PART_ColumnResizerThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_ColumnResizerThumbLeft", Type = typeof(Thumb))]
    public class FilterCell : Cell
    {
        static FilterCell()
        {
            UIElement.FocusableProperty.OverrideMetadata(typeof(FilterCell), new FrameworkPropertyMetadata(true));
            Cell.ReadOnlyProperty.OverrideMetadata(typeof(FilterCell), new FrameworkPropertyMetadata(false));

            FilterCell.IsPressedProperty = FilterCell.IsPressedPropertyKey.DependencyProperty;
        }

        public FilterCell()
        {
            this.ReadOnly = true;
        }

        #region IsPressed Read-Only Property

        private static readonly DependencyPropertyKey IsPressedPropertyKey =
          DependencyProperty.RegisterReadOnly("IsPressed", typeof(bool), typeof(FilterCell), new PropertyMetadata(false));

        public static readonly DependencyProperty IsPressedProperty;

        public bool IsPressed
        {
            get
            {
                return (bool)this.GetValue(FilterCell.IsPressedProperty);
            }
        }

        private void SetIsPressed(bool value)
        {
            this.SetValue(FilterCell.IsPressedPropertyKey, value);
        }

        #endregion IsPressed Read-Only Property

        #region DataGridContext Internal Read-Only Property

        internal DataGridContext DataGridContext
        {
            get
            {
                return m_dataGridContext;
            }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public virtual void LoadFilter()
        {
        }

        protected override void InitializeCore(DataGridContext dataGridContext, Row parentRow, ColumnBase parentColumn)
        {
            base.InitializeCore(dataGridContext, parentRow, parentColumn);
        }

        protected internal override void PrepareDefaultStyleKey(ViewBase view)
        {
            var newThemeKey = view.GetDefaultStyleKey(typeof(FilterCell));
            if (object.Equals(this.DefaultStyleKey, newThemeKey))
                return;

            this.DefaultStyleKey = newThemeKey;
        }

        protected internal override void PrepareContainer(DataGridContext dataGridContext, object item)
        {
            base.PrepareContainer(dataGridContext, item);
            m_dataGridContext = dataGridContext;
        }

        protected internal override void ClearContainer()
        {
            m_dataGridContext = null;
            base.ClearContainer();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DataGridContext dataGridContext = DataGridControl.GetDataGridContext(this);

            if (this.CaptureMouse())
            {
                this.SetIsPressed(true);

                e.Handled = true;
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if ((this.IsMouseCaptured) && (e.LeftButton == MouseButtonState.Pressed))
            {
                {
                    this.SetIsPressed(false);
                }

                e.Handled = true;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            bool isMouseCaptured = this.IsMouseCaptured;
            bool isPressed = this.IsPressed;

            if (isMouseCaptured)
            {
                bool click = isPressed;

                this.ReleaseMouseCapture();
                this.SetIsPressed(false);

                e.Handled = true;
            }

            // Focus must be done only on mouse up ( after the sort is done ... etc )
            // we have to focus the grid.

            // We don't need to set PreserveEditorFocus to true since clicking on another element will automatically
            // set the Cell/Row IsBeingEdited to false and try to make it leave edition.
            DataGridContext dataGridContext = DataGridControl.GetDataGridContext(this);

            if (dataGridContext != null)
            {
                DataGridControl dataGridControl = dataGridContext.DataGridControl;

                if ((dataGridControl != null) && (!dataGridControl.IsKeyboardFocusWithin))
                {
                    dataGridControl.Focus();
                }
            }

            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (this.IsPressed)
            {
                this.SetIsPressed(false);
            }

            base.OnLostMouseCapture(e);
        }

        private FilterCell GetPreviousVisibleFilterCell()
        {
            var parentColumn = this.ParentColumn;
            if (parentColumn == null)
                return null;

            var previousVisibleColumn = parentColumn.PreviousVisibleColumn;
            if (previousVisibleColumn == null)
                return null;

            return (FilterCell)this.ParentRow.Cells[previousVisibleColumn];
        }

        private void SetColumnBinding(DependencyProperty targetProperty, string sourceProperty)
        {
            if (BindingOperations.GetBinding(this, targetProperty) != null)
                return;

            var binding = FilterCell.CreateColumnBinding(sourceProperty);
            if (binding != null)
            {
                BindingOperations.SetBinding(this, targetProperty, binding);
            }
            else
            {
                BindingOperations.ClearBinding(this, targetProperty);
            }
        }

        private static Binding CreateColumnBinding(string sourceProperty)
        {
            var binding = new Binding();
            binding.Path = new PropertyPath(sourceProperty);
            binding.Mode = BindingMode.OneWay;
            binding.RelativeSource = new RelativeSource(RelativeSourceMode.Self);
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            return binding;
        }

        private DataGridContext m_dataGridContext; // = null;

        private const double MIN_WIDTH = 8d;
        private double m_originalWidth = -1d;
        private Thumb m_columnResizerThumb; // = null
        private Thumb m_columnResizerThumbLeft; // null

        #region MillisecondsConverter Private Class

        private sealed class MillisecondsConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return TimeSpan.FromMilliseconds(System.Convert.ToDouble(value));
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        #endregion
    }
}
