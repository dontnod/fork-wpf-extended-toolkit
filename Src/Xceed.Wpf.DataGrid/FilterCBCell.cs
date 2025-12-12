using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;

namespace Xceed.Wpf.DataGrid
{
  /// <summary>
  /// VM class for Checkable filter Items
  /// </summary>
  public class FilterItem : INotifyPropertyChanged
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
        if (m_isChecked != value)
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

  /// <summary>
  /// Control class for Combobox Filter Cells
  /// </summary>
  public class FilterCBCell : FilterCell, IEditableObject
  {
    static FilterCBCell()
    {
    }

    public FilterCBCell()
    {
      ReadOnly = true;
      ItemsFilters = new List<FilterItem>();
      ClearAll = new ActionCommand((o) => OnClearAll());
      CheckAll = new ActionCommand((o) => OnCheckAll());
    }

    #region Bindable Properties

    #region ItemSource Property

    public static readonly DependencyProperty ItemsFiltersProperty = DependencyProperty.Register(
      "ItemsFilters",
      typeof(List<FilterItem>),
      typeof(FilterCBCell),
      new UIPropertyMetadata(null));

    public List<FilterItem> ItemsFilters
    {
      get
      {
        return (List<FilterItem>)GetValue(FilterCBCell.ItemsFiltersProperty);
      }
      set
      {
        SetValue(FilterCBCell.ItemsFiltersProperty, value);
      }
    }

    #endregion

    #region HasFilters Property

    private bool HasFilters => ItemsFilters.Any(x => x.IsChecked);

    #endregion

    #region SelectedFilters Property

    private List<string> SelectedFilters => ItemsFilters.Where(x => x.IsChecked).Select(x => x.Item.ToString()).ToList();

    #endregion

    #region FilterContent Property

    /// <summary>
    /// Property used for serializing Filters
    /// </summary>
    public static readonly DependencyProperty FilterContentProperty = DependencyProperty.Register(
      "FilterContent",
      typeof(string),
      typeof(FilterCBCell),
      new UIPropertyMetadata(null));

    public string FilterContent
    {
      get
      {
        return (string)GetValue(FilterCBCell.FilterContentProperty);
      }
      set
      {
        SetValue(FilterCBCell.FilterContentProperty, value);
      }
    }

    #endregion

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

    private bool _isLoading = false;

    public ICommand ClearAll { get; set; }

    public ICommand CheckAll { get; set; }

    /// <summary>
    /// Update FilterContent property
    /// </summary>
    private void UpdateContent()
    {
      var checkedItems = ItemsFilters.Where(x => x.IsChecked);
      if (!checkedItems.Any())
        FilterContent = String.Empty;
      else
      {
        var content = string.Empty;
        foreach (var item in checkedItems)
          content += item.Item.ToString() + ", ";
        FilterContent = content.Substring(0, content.Length - 2);
      }
    }

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();
    }

    /// <summary>
    /// Update Filter Items UI
    /// </summary>
    public override void LoadFilter()
    {
      _isLoading = true;
      OnClearAll();

      FilterRow fRow = ParentRow as FilterRow;
      if (fRow != null)
      {
        ListFilter filter = fRow.GetFilter(ParentColumn.FieldName) as ListFilter;
        if (filter != null && filter.Filters.Count > 0)
        {
          //Init itemsfilterschecked;
          foreach (FilterItem item in ItemsFilters)
          {
            item.IsChecked = filter.Filters.Contains(item.Item.ToString());
          }
        }
      }

      UpdateContent();
      _isLoading = false;
    }

    /// <summary>
    /// Initialize Filters
    /// </summary>
    protected override void InitializeCore(DataGridContext dataGridContext, Row parentRow, ColumnBase parentColumn)
    {
      base.InitializeCore(dataGridContext, parentRow, parentColumn);

      ColumnComboBox cbCol = parentColumn as ColumnComboBox;

      if (cbCol != null && cbCol.ItemsList != null)
      {
        var filters = new List<FilterItem>();

        // Add no value option
        filters.Add(new FilterItem(ListFilter.NO_VALUE_FILTER, false));

        foreach (var item in cbCol.ItemsList)
        {
          FilterItem fi = new FilterItem(item, false);
          filters.Add(fi);
        }

        filters.ForEach(x => x.PropertyChanged += FilterItemChanged);

        ItemsFilters.ForEach(x => x.PropertyChanged -= FilterItemChanged);
        ItemsFilters = filters;

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

      ColumnBase parentColumn = ParentColumn;
      if ((parentColumn == null) || (!parentColumn.AllowFilter))
        return false;

      return true;
    }

    /// <summary>
    /// Filter Items have been modified (checkbox clicked)
    /// </summary>
    private void FilterItemChanged(object sender, PropertyChangedEventArgs e)
    {
      if (!CanDoFilter() || _isLoading)
        return;

      FilterRow fr = ParentRow as FilterRow;
      DataGridContext dataGridContext = DataGridContext;

      Debug.Assert(dataGridContext != null);

      if (fr != null)
      {
        if (HasFilters)
          fr.AddFilter(ParentColumn.FieldName, new ListFilter(SelectedFilters));
        else
          fr.RemoveFilter(ParentColumn.FieldName);
        dataGridContext.Items.Filter = new Predicate<object>(fr.ApplyTotalFilter);
      }

      UpdateContent();
    }

    private void OnClearAll()
    {
      foreach (var item in ItemsFilters)
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
