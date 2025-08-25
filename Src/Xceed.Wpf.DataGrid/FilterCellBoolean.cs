using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;

namespace Xceed.Wpf.DataGrid
{
    public class FilterCellBoolean : FilterCell, IEditableObject
    {
        static FilterCellBoolean()
        {
        }

        public FilterCellBoolean()
        {
            this.ReadOnly = true;
            this.FilterValue = null;
        }

        private bool HasFilters => FilterValue != null;

        #region FilterValue Property

        public static readonly DependencyProperty FilterValueProperty = DependencyProperty.Register(
          "FilterValue",
          typeof(bool?),
          typeof(FilterCellBoolean),
          new UIPropertyMetadata(null));

        public bool? FilterValue
        {
          get => (bool?)GetValue(FilterCellBoolean.FilterValueProperty);
          set => SetValue(FilterCellBoolean.FilterValueProperty, value);
        }

    #endregion

        private bool _isLoading = false;

        public override void OnApplyTemplate()
        {
          base.OnApplyTemplate();

          System.Windows.Controls.CheckBox cb = GetTemplateChild("CellPresenter") as System.Windows.Controls.CheckBox;
          cb.Checked += FilterCheckedChanged;
          cb.Unchecked += FilterCheckedChanged;
          cb.Indeterminate += FilterCheckedChanged;
        }

        private void FilterCheckedChanged(object sender, RoutedEventArgs e)
        {
          if (!CanDoFilter() || _isLoading)
            return;

          System.Windows.Controls.CheckBox cb = e.Source as System.Windows.Controls.CheckBox;
          if (cb == null)
            return;

          bool? isChecked = cb.IsChecked;

          FilterRow fRow = ParentRow as FilterRow;
          DataGridContext dataGridContext = this.DataGridContext;

          Debug.Assert(dataGridContext != null);

          if (fRow != null)
          {
            fRow.AddFilter(ParentColumn.FieldName, new BooleanFilter(isChecked));
            dataGridContext.Items.Filter = new Predicate<object>(fRow.ApplyTotalFilter);
          }
        }

        public override void LoadFilter()
        {
            _isLoading = true;
            FilterValue = null;
            var fRow = ParentRow as FilterRow;

            if (fRow != null)
            {
                var filter = fRow.GetFilter(ParentColumn.FieldName) as BooleanFilter;
                if (filter != null )
                    FilterValue = filter.Filter;
            }

            _isLoading = false;
        }

        protected override void InitializeCore(DataGridContext dataGridContext, Row parentRow, ColumnBase parentColumn)
        {
            base.InitializeCore(dataGridContext, parentRow, parentColumn);

            //Initialize filter if already registered on filter row. 
            LoadFilter();
        }

        protected internal override void PrepareDefaultStyleKey(ViewBase view)
        {
            var newThemeKey = view.GetDefaultStyleKey(typeof(FilterCellBoolean));
            if (Equals(DefaultStyleKey, newThemeKey))
                return;

            DefaultStyleKey = newThemeKey;
        }

        internal bool CanDoFilter()
        {
            FilterRow parentRow = ParentRow as FilterRow;

            DataGridContext dataGridContext = DataGridContext;
            if (dataGridContext == null)
                return false;

            // When details are flatten, only the FilterCBCell at the master level may do the sort.
            if (dataGridContext.IsAFlattenDetail)
                return false;

            if (dataGridContext.SourceDetailConfiguration == null)
            {
                if (!dataGridContext.Items.CanFilter)
                    return false;
            }

            if (!this.IsEnabled)
                return false;

            ColumnBase parentColumn = this.ParentColumn;
            if ((parentColumn == null) || (!parentColumn.AllowFilter))
                return false;

            return true;
        }
    }
}
