﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;

namespace Xceed.Wpf.DataGrid
{
    public class FilterItem: INotifyPropertyChanged
    {
        public FilterItem(object item, bool isChecked)
        {
            Item = item;
            IsChecked = isChecked;
        }

        private bool m_isChecked;
        public bool IsChecked
        {
            get
            {
                return m_isChecked;
            }
            set
            {
                if(m_isChecked != value)
                {
                    m_isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }
        public object Item
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }

    public class FilterCBCell : FilterCell, IEditableObject
    {
        static FilterCBCell()
        {
        }

        public FilterCBCell()
        {
            this.ReadOnly = true;
            this.ItemsFilters = new ObservableCollection<FilterItem>();
            this.ClearAll = new ActionCommand((o) => OnClearAll());
            this.CheckAll = new ActionCommand((o) => OnCheckAll());
        }

        //#region IsPressed Read-Only Property

        //private static readonly DependencyPropertyKey IsPressedPropertyKey =
        //  DependencyProperty.RegisterReadOnly("IsPressed", typeof(bool), typeof(FilterCBCell), new PropertyMetadata(false));

        //public static readonly DependencyProperty IsPressedProperty;

        //public bool IsPressed
        //{
        //    get
        //    {
        //        return (bool)this.GetValue(FilterCBCell.IsPressedProperty);
        //    }
        //}

        //private void SetIsPressed(bool value)
        //{
        //    this.SetValue(FilterCBCell.IsPressedPropertyKey, value);
        //}

        //#endregion IsPressed Read-Only Property

        #region ItemSource Property

        public static readonly DependencyProperty ItemsFiltersProperty = DependencyProperty.Register(
          "ItemsFilters",
          typeof(ObservableCollection<FilterItem>),
          typeof(FilterCBCell),
          new UIPropertyMetadata(null));

        public ObservableCollection<FilterItem> ItemsFilters
        {
            get
            {
                return (ObservableCollection<FilterItem>)this.GetValue(FilterCBCell.ItemsFiltersProperty);
            }
            set
            {
                this.SetValue(FilterCBCell.ItemsFiltersProperty, value);
            }
        }

        #endregion

        #region HasFilters Property

        private bool HasFilters => ItemsFilters.Any(x => x.IsChecked);

        #endregion

        #region SelectedFilters Property

        private List<string> SelectedFilters => ItemsFilters.Where(x => x.IsChecked).Select(x => x.Item.ToString()).ToList();

        #endregion

        #region DisplayNamePath Property

        public static readonly DependencyProperty DisplayNamePathProperty = DependencyProperty.Register(
          "DisplayNamePath",
          typeof(string),
          typeof(FilterCBCell),
          new UIPropertyMetadata(null));

        public string DisplayNamePath
        {
            get
            {
                return (string)this.GetValue(FilterCBCell.DisplayNamePathProperty);
            }
            set
            {
                this.SetValue(FilterCBCell.DisplayNamePathProperty, value);
            }
        }

        #endregion

        #region CanBeCollapsed Property

        internal override bool CanBeCollapsed
        {
            get
            {

                var parentColumn = this.ParentColumn;
                if (parentColumn == null)
                    return true;

                return !TableflowView.GetIsBeingDraggedAnimated(parentColumn);

            }
        }

    #endregion

        public ICommand ClearAll { get; set; }

        public ICommand CheckAll { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public override void LoadFilter()
        {
            OnClearAll();

            FilterRow fRow = ParentRow as FilterRow;
            if(fRow != null)
            {
                ListFilter filter = fRow.GetFilter(ParentColumn.FieldName) as ListFilter;
                if(filter != null && filter.Filters.Count > 0)
                {
                    //Init itemsfilterschecked;
                    foreach(FilterItem item in ItemsFilters)
                    {
                        item.IsChecked = filter.Filters.Contains(item.Item.ToString());
                    }
                }
            }
        }

        protected override void InitializeCore(DataGridContext dataGridContext, Row parentRow, ColumnBase parentColumn)
        {
            base.InitializeCore(dataGridContext, parentRow, parentColumn);

            ColumnComboBox cbCol = parentColumn as ColumnComboBox;

            if(cbCol != null)
            {
                this.ItemsFilters.Clear();

                foreach (var item in cbCol.ItemsList)
                {
                    FilterItem fi = new FilterItem(item, false);
                    fi.PropertyChanged += FilterItemChanged;
                    this.ItemsFilters.Add(fi);
                }

                //Initialize filter if already registered on filter row. 
                LoadFilter();
            }
        }

        protected internal override void PrepareDefaultStyleKey(ViewBase view)
        {
            var newThemeKey = view.GetDefaultStyleKey(typeof(FilterCBCell));
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

        private void FilterItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!this.CanDoFilter())
                return;

            FilterRow fr = this.ParentRow as FilterRow;
            DataGridContext dataGridContext = this.DataGridContext;

            Debug.Assert(dataGridContext != null);

            if (fr != null)
            {
                if (HasFilters)
                    fr.AddFilter(ParentColumn.FieldName, new ListFilter(SelectedFilters));
                else
                    fr.RemoveFilter(ParentColumn.FieldName);
                dataGridContext.Items.Filter = new Predicate<object>(fr.ApplyTotalFilter);
            }
        }

        private void OnClearAll()
        {
          foreach(var item in ItemsFilters)
          {
            item.IsChecked = false;
          }
        }

        private void OnCheckAll()
        {
          foreach (var item in ItemsFilters)
          {
            item.IsChecked = true;
          }
        }

    }
}
