using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xceed.Utils.Wpf.DragDrop;
using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using Xceed.Wpf.DataGrid.Utils.JsonSerialization;

namespace Xceed.Wpf.DataGrid
{
  public class FilterRow : Row
  {
    static FilterRow()
    {
      Row.NavigationBehaviorProperty.OverrideMetadata(typeof(FilterRow), new FrameworkPropertyMetadata(NavigationBehavior.None));
      FrameworkElement.ContextMenuProperty.OverrideMetadata(typeof(FilterRow), new FrameworkPropertyMetadata(null, new CoerceValueCallback(FilterRow.CoerceContextMenu)));
    }

    public FilterRow()
    {
      //ensure that all FilterRows are ReadOnly
      this.ReadOnly = true; //This is safe to perform since there is nowhere a callback installed on the DP... 

      //ensure that all FilterRows are not navigable
      this.NavigationBehavior = NavigationBehavior.None; //This is safe to perform since there is nowhere a callback installed on the DP...
    }

    #region Configuration Internal Property

    internal ColumnManagerRowConfiguration Configuration
    {
      get
      {
        return m_configuration;
      }
      set
      {
        if (value == m_configuration)
          return;

        if (m_configuration != null)
        {
          PropertyChangedEventManager.RemoveListener(m_configuration, this, string.Empty);
        }

        m_configuration = value;

        if (m_configuration != null)
        {
          PropertyChangedEventManager.AddListener(m_configuration, this, string.Empty);
        }
      }
    }

    private ColumnManagerRowConfiguration m_configuration;

    #endregion

    #region Filtering

    private Dictionary<string, IFilter> _currentFilters = new Dictionary<string, IFilter>();
    public Dictionary<string, IFilter> CurrentFilters
    {
      get => _currentFilters;
      set
      {
        if (value != _currentFilters)
        {
          _currentFilters = value;
          this.OnPropertyChanged(nameof(CurrentFilters));
        }
      }
    }

    public void AddFilter(string field, IFilter filter)
    {
      if (CurrentFilters.ContainsKey(field))
        CurrentFilters[field] = filter;
      else
        CurrentFilters.Add(field, filter);
      OnPropertyChanged(nameof(CurrentFilters));
    }

    public void AddColumnFilter(string header, string filtervalue, FilterTypes filterType)
    {
      if (filtervalue == string.Empty)
        return;

      IFilter filter = FilterFactory.CreateFilter(filterType, filtervalue);

      if (CurrentFilters.ContainsKey(header))
        CurrentFilters[header] = filter;
      else
        CurrentFilters.Add(header, filter);

      OnPropertyChanged(nameof(CurrentFilters));
      UpdateFilterCells();
    }

    public string GetColumnFilter(string header)
    {
      IFilter filter;
      _currentFilters.TryGetValue(header, out filter);
      return filter?.ToString();
    }

    public void RemoveFilter(string field)
    {
      CurrentFilters.Remove(field);
      OnPropertyChanged(nameof(CurrentFilters));
    }

    public IFilter GetFilter(string field)
    {
      IFilter filter;
      _currentFilters.TryGetValue(field, out filter);
      return filter;
    }

    public void ClearFilters()
    {
      CurrentFilters.Clear();
      OnPropertyChanged(nameof(CurrentFilters));

      UpdateFilterCells();
    }

    public bool ApplyTotalFilter(object obj)
    {
      foreach (string field in _currentFilters.Keys)
      {
        if (!_currentFilters[field].ApplyFilter(obj, field))
          return false;
      }
      return true;
    }

    public void LoadFilters(Dictionary<string, IFilter> filter)
    {
      if (_currentFilters.Count == filter.Count && !_currentFilters.Except(filter).Any())
        return;

      CurrentFilters = filter;
      UpdateFilterCells();
    }

    private void UpdateFilterCells()
    {
      //update filter cells
      foreach (FilterCell cell in CreatedCells)
      {
        cell.LoadFilter();
      }

      FixedCellPanel fcp = CellsHostPanel as FixedCellPanel;
      if (fcp != null)
      {
        fcp.DataGridContext.Items.Filter = new Predicate<object>(ApplyTotalFilter);
      }
    }
    #endregion

    protected override Cell CreateCell(ColumnBase column)
    {
      if (column as ColumnComboBox != null)
      {
        return new FilterCBCell();
      }

      return new FilterCellText();
    }

    protected override bool IsValidCellType(Cell cell)
    {
      return (cell is FilterCell);
    }

    protected internal override void PrepareDefaultStyleKey(Xceed.Wpf.DataGrid.Views.ViewBase view)
    {
      var currentThemeKey = view.GetDefaultStyleKey(typeof(FilterRow));
      if (currentThemeKey.Equals(this.DefaultStyleKey))
        return;

      this.DefaultStyleKey = currentThemeKey;
    }

    protected override void PrepareContainer(DataGridContext dataGridContext, object item)
    {
      base.PrepareContainer(dataGridContext, item);

      this.Configuration = dataGridContext.ColumnManagerRowConfiguration;
    }

    protected override void ClearContainer()
    {
      this.Configuration = null;

      base.ClearContainer();
    }

    protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
      //Do not call the base class implementation to prevent SetCurrent from being called... 
      //This is because we do not want the ColumnManager Row to be selectable through the Mouse
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
      if (e.Handled)
        return;

      base.OnMouseRightButtonUp(e);
    }

    private static object CoerceContextMenu(DependencyObject sender, object value)
    {
      if (value == null)
        return value;

      var self = sender as FilterRow;
      if ((self == null))
        return value;

      return null;
    }

    private void OnConfigurationPropertyChanged(PropertyChangedEventArgs e)
    {
      var propertyName = e.PropertyName;

    }

    #region IWeakEventListener Members

    protected override bool OnReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
      var result = base.OnReceiveWeakEvent(managerType, sender, e);

      if (typeof(PropertyChangedEventManager) == managerType)
      {
        if (sender == m_configuration)
        {
          this.OnConfigurationPropertyChanged((PropertyChangedEventArgs)e);
        }
      }
      else
      {
        return result;
      }

      return true;
    }

    #endregion
  }
}
