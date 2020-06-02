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
    public class FilterCellText: FilterCell, IEditableObject
    {
        static FilterCellText()
        {
        }

        public FilterCellText()
        {
            this.ReadOnly = true;
            this.FilterText = "";
        }


        #region HasFilters Property

        private bool HasFilters => FilterText != "";

        #endregion

        #region FilterText Property

        public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register(
          "FilterText",
          typeof(string),
          typeof(FilterCellText),
          new UIPropertyMetadata(null));

        public string FilterText
        {
            get
            {
                return (string)this.GetValue(FilterCellText.FilterTextProperty);
            }
            set
            {
                this.SetValue(FilterCellText.FilterTextProperty, value);
            }
        }

    #endregion

        private bool _isLoading = false;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            System.Windows.Controls.TextBox tb = this.GetTemplateChild("CellPresenter") as System.Windows.Controls.TextBox;
            tb.TextChanged += FilterChanged;
        }

        public override void LoadFilter()
        {
            _isLoading = true;
            FilterText = string.Empty;

            FilterRow fRow = ParentRow as FilterRow;
            if (fRow != null)
            {
                TextFilter filter = fRow.GetFilter(ParentColumn.FieldName) as TextFilter;
                if (filter != null )
                {
                    FilterText = filter.Filter;
                }
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
            var newThemeKey = view.GetDefaultStyleKey(typeof(FilterCellText));
            if (object.Equals(this.DefaultStyleKey, newThemeKey))
                return;

            this.DefaultStyleKey = newThemeKey;
        }

        internal bool CanDoFilter()
        {
            FilterRow parentRow = this.ParentRow as FilterRow;

            DataGridContext dataGridContext = this.DataGridContext;
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

        private void FilterChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!this.CanDoFilter() || _isLoading)
                return;

            //FilterText = e.Source.ToString();
            System.Windows.Controls.TextBox tb = e.Source as System.Windows.Controls.TextBox;
            if (tb == null)
                return;

            string text = tb.Text;

            FilterRow fr = this.ParentRow as FilterRow;
            DataGridContext dataGridContext = this.DataGridContext;

            Debug.Assert(dataGridContext != null);

            if (fr != null)
            {
                if (text != "")
                    fr.AddFilter(ParentColumn.FieldName, new TextFilter(text));
                else
                    fr.RemoveFilter(ParentColumn.FieldName);
                dataGridContext.Items.Filter = new Predicate<object>(fr.ApplyTotalFilter);
            }
        }
    }
}
