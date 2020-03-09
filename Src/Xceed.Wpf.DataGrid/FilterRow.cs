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

    private Dictionary<string, IFilter> m_filters = new Dictionary<string, IFilter>();

    public void AddFilter(string field, IFilter filter)
    {
      if (m_filters.ContainsKey(field))
        m_filters[field] = filter;
      else
        m_filters.Add(field, filter);
    }

    public void RemoveFilter(string field)
    {
      m_filters.Remove(field);
    }

    public IFilter GetFilter(string field)
    {
      IFilter filter;
      m_filters.TryGetValue(field, out filter);
      return filter;
    }

    public void ClearFilters()
    {
      m_filters.Clear();

      UpdateFilters();
    }

    public bool ApplyTotalFilter(object obj)
    {
      foreach (string field in m_filters.Keys)
      {
        if (!m_filters[field].ApplyFilter(obj, field))
          return false;
      }
      return true;
    }

    public string SaveFilters()
    {
      using (var memoryStream = new MemoryStream())
      {
        using (var writer = new StreamWriter(memoryStream))
        {
          var serializer = new JsonSerializer();
          serializer.Formatting = Formatting.Indented;
          serializer.TypeNameHandling = TypeNameHandling.Auto;
          serializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
          serializer.Serialize(writer, m_filters);

          writer.Flush();
          memoryStream.Position = 0;

          var reader = new StreamReader(memoryStream);
          return reader.ReadToEnd();
        }
      }


      ////using (var file = File.CreateText(filename))
      //{
      //  //var serializer = new JsonSerializer();
      //  //serializer.Formatting = Formatting.Indented;
      //  //serializer.TypeNameHandling = TypeNameHandling.Auto;
      //  //serializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;

      //  string filterString = JsonConvert.SerializeObject(m_filters, Formatting.Indented);
      //}
    }

    public void LoadFilters(string filter)
    {
      try
      {
        //read filter file
        var settings = new JsonSerializerSettings();
        settings.TypeNameHandling = TypeNameHandling.Auto;                      //default
        settings.NullValueHandling = NullValueHandling.Include;                 //default
        settings.ObjectCreationHandling = ObjectCreationHandling.Replace;       //not default
        settings.PreserveReferencesHandling = PreserveReferencesHandling.None;  //default

        m_filters = JsonConvert.DeserializeObject<Dictionary<string, IFilter>>(filter);

        UpdateFilters();
      }
      catch (JsonReaderException e)
      {
      }
      catch (JsonSerializationException e)
      {
      }
      catch (Exception e)
      {
      }
    }

    private void UpdateFilters()
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
