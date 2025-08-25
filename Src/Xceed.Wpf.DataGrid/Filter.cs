using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xceed.Wpf.DataGrid.Utils.JsonSerialization;

namespace Xceed.Wpf.DataGrid
{
  [JsonConverter(typeof(FilterJsonConverter))]

  public enum FilterType
  {
    List,
    Text,
    Boolean
  }

  public abstract class IFilter
  {
    public FilterType FilterType { get; set; }
    public abstract bool ApplyFilter(object obj, string property);
    public abstract string ToString();
  }

  public class ListFilter : IFilter
  {
    public List<string> Filters { get; set; } = new List<string>();
    public bool ShouldSerializeFilters() => Filters.Any();

    public ListFilter(List<string> filters)
    {
      FilterType = FilterType.List;
      Filters = new List<string>(filters);
    }

    public override bool ApplyFilter(object obj, string propertyName)
    {
      if (Filters.Count == 0)
        return true;

      string value = string.Empty;

      var propertyParts = propertyName.Split(':');
      // check is nested property
      if (propertyParts.Length == 2)
      {
        var dicProperty = obj.GetType().GetProperty(propertyParts[0])?.GetValue(obj, null) as Dictionary<string, string>;
        if (dicProperty == null)
          return false;
        dicProperty.TryGetValue(propertyParts[1], out value);
      }
      else if (propertyParts.Length == 3)
      {
        var dicProperty = obj.GetType().GetProperty(propertyParts[0])?.GetValue(obj, null) as Dictionary<string, Dictionary<string, string>>;
        if (dicProperty == null)
          return false;
        dicProperty.TryGetValue(propertyParts[1], out var subDicProp);
        subDicProp?.TryGetValue(propertyParts[2], out value);
      }
      else
      {
        if (obj is ICustomTypeDescriptor objDesc)
        {
          var property = objDesc.GetProperties().OfType<PropertyDescriptor>().FirstOrDefault(x => x.Name == propertyName);
          value = property?.GetValue(obj)?.ToString();
        }
        else
        {
          var property = obj.GetType().GetProperty(propertyName);
          value = property?.GetValue(obj, null)?.ToString();
        }
      }

      if (value == null)
        return false;

      return Filters.Any(x => x == value);
    }

    public override string ToString() => Filters.FirstOrDefault();
  }

  public class BooleanFilter : IFilter
  {
    public bool? Filter { get; set; } = null;
    public bool ShouldSerializeFilter() => Filter != null;

    public BooleanFilter(bool? filterValue)
    {
      FilterType = FilterType.Boolean;
      Filter = filterValue;
    }

    public override bool ApplyFilter(object obj, string propertyName)
    {
      if (Filter == null)
        return true;

      string value = string.Empty;

      var propertyParts = propertyName.Split(':');
      // check is nested property
      if (propertyParts.Length == 2)
      {
        var dicProperty = obj.GetType().GetProperty(propertyParts[0])?.GetValue(obj, null) as Dictionary<string, string>;
        if (dicProperty == null)
          return false;
        dicProperty.TryGetValue(propertyParts[1], out value);
      }
      else if (propertyParts.Length == 3)
      {
        var dicProperty = obj.GetType().GetProperty(propertyParts[0])?.GetValue(obj, null) as Dictionary<string, Dictionary<string, string>>;
        if (dicProperty == null)
          return false;
        dicProperty.TryGetValue(propertyParts[1], out var subDicProp);
        subDicProp?.TryGetValue(propertyParts[2], out value);
      }
      else
      {
        if (obj is ICustomTypeDescriptor objDesc)
        {
          var property = objDesc.GetProperties().OfType<PropertyDescriptor>().FirstOrDefault(x => x.Name == propertyName);
          value = property?.GetValue(obj)?.ToString();
        }
        else
        {
          var property = obj.GetType().GetProperty(propertyName);
          value = property?.GetValue(obj, null)?.ToString();
        }
      }

      if (value == null) return false;
      if (value == "") return true;

      return value.ToString() == Filter.ToString();
    }

    public override string ToString() => Filter?.ToString() ?? "";
  }

  public class TextFilter : IFilter
  {
    public enum BlockOperation
    {
      Or,
      And
    }

    private static char s_AND = '&';
    private static char s_OR = '|';
    private static string s_NULL = "\"\"";

    private static Regex s_SYNTAX = new Regex("\"(.*?)\"");
    private static Regex s_EQUAL = new Regex("^\"(.*?)\"$");
    private static Regex s_CONTAINS = new Regex("^\"\\*(.*?)\\*\"$");

    private static string[] s_KEYWORDS = {
      s_AND.ToString(),
      s_OR.ToString(),
      s_NULL
    };
    private static string[] s_ORAND = { s_AND.ToString(), s_OR.ToString() };

    private List<Tuple<BlockOperation, string>> m_filterblocks;
    private bool m_hasMacro;

    public string Filter { get; set; } = string.Empty;
    public bool ShouldSerializeFilter() => !string.IsNullOrEmpty(Filter);

    public TextFilter(string filter)
    {
      FilterType = FilterType.Text;

      Filter = filter;//.ToLower();
      m_hasMacro = HasMacro();
      BuildFilterBlock();
    }

    public override bool ApplyFilter(object obj, string propertyName)
    {
      if (Filter == "")
        return true;

      string value = string.Empty;

      var propertyParts = propertyName.Split(':');
      // check is nested property
      if (propertyParts.Length == 2)
      {
        var dicProperty = obj.GetType().GetProperty(propertyParts[0])?.GetValue(obj, null) as Dictionary<string, string>;
        if (dicProperty == null)
          return false;
        dicProperty.TryGetValue(propertyParts[1], out value);
        value = value?.ToLower();
      }
      else if (propertyParts.Length == 3)
      {
        var dicProperty = obj.GetType().GetProperty(propertyParts[0])?.GetValue(obj, null) as Dictionary<string, Dictionary<string, string>>;
        if (dicProperty == null)
          return false;
        dicProperty.TryGetValue(propertyParts[1], out Dictionary<string, string> subDicProp);
        subDicProp?.TryGetValue(propertyParts[2], out value);
        value = value?.ToLower();
      }
      else
      {
        if (obj is ICustomTypeDescriptor objDesc)
        {
          var property = objDesc.GetProperties().OfType<PropertyDescriptor>().FirstOrDefault(x => x.Name == propertyName);
          value = property?.GetValue(obj)?.ToString().ToLower();
        }
        else
        {
          var property = obj.GetType().GetProperty(propertyName);
          value = property?.GetValue(obj, null)?.ToString().ToLower();
        }
      }

      if (value == null)
        return false;

      if (m_hasMacro) //contains macro
      {
        return ApplyMacro(value);
      }
      else // no macro
      {
        return value.Contains(Filter.ToLower());
      }
    }

    public override string ToString() => Filter;

    private bool HasMacro()
    {
      if (Filter == null || Filter == string.Empty)
        return false;
      if (s_KEYWORDS.Any(Filter.Contains))
        return true;
      if (s_SYNTAX.IsMatch(Filter))
        return true;
      return false;
    }

    private void BuildFilterBlock()
    {
      m_filterblocks = new List<Tuple<BlockOperation, string>>();

      if (!m_hasMacro)
        return;

      //only one block
      if (!s_ORAND.Any(Filter.Contains))
      {
        m_filterblocks.Add(new Tuple<BlockOperation, string>(BlockOperation.And, Filter));
        return;
      }

      string tempFilter = Filter;
      BlockOperation prevOp = BlockOperation.And;
      int prevOpIdx = (Filter[0] == s_AND || Filter[0] == s_OR) ? 1 : 0;

      //scan whole string for AND and OR
      for (int c = prevOpIdx; c < Filter.Length; ++c)
      {
        if (Filter[c] == s_AND || Filter[c] == s_OR)
        {
          //Add block
          string block = tempFilter.Substring(prevOpIdx, c - prevOpIdx).Trim();
          m_filterblocks.Add(new Tuple<BlockOperation, string>(prevOp, block));

          prevOp = (Filter[c] == s_AND) ? BlockOperation.And : BlockOperation.Or;
          prevOpIdx = c + 1;
        }
      }
      //last block
      m_filterblocks.Add(new Tuple<BlockOperation, string>(prevOp, tempFilter.Substring(prevOpIdx).Trim()));
    }

    private bool ApplyMacro(string value)
    {
      bool result = true;

      foreach (var block in m_filterblocks)
      {
        if (block.Item1 == BlockOperation.And)
          result = result && ApplyMacroBlock(block.Item2, value);
        else
          result = result || ApplyMacroBlock(block.Item2, value);
      }

      return result;
    }

    private bool ApplyMacroBlock(string filterBlock, string value)
    {
      if (filterBlock == string.Empty)
        return false;

      if (filterBlock == s_NULL)
        return value == "";

      //contains filter
      if (s_CONTAINS.IsMatch(filterBlock))
      {
        string actualFilter = s_CONTAINS.Match(filterBlock).Groups["1"].Value;
        return value.Contains(actualFilter);
      }
      //equal filter
      if (s_EQUAL.IsMatch(filterBlock))
      {
        string actualFilter = s_EQUAL.Match(filterBlock).Groups["1"].Value;
        return value == actualFilter;
      }

      return value.Contains(filterBlock);
    }
  }

  static public class FilterFactory
  {
    static public IFilter CreateFilter(FilterType filterType, object filterValue)
    {
      switch (filterType)
      {
        case FilterType.List:
          var filters = new List<string>() { (string)filterValue };
          return new ListFilter(filters);

        case FilterType.Boolean:
          return new BooleanFilter((bool?)filterValue);

        case FilterType.Text:
        default:
          return new TextFilter((string)filterValue);
      }
    }
  }
}
