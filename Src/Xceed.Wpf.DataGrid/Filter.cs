using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Xceed.Wpf.DataGrid
{
  public abstract class IFilter
  {
    public abstract bool ApplyFilter(object obj, string property);

    public abstract void SerializeFilter();
  }

  public class ListFilter : IFilter
  {
    public List<string> Filters;

    public ListFilter(List<string> filters)
    {
      Filters = new List<string>(filters);
    }

    public override bool ApplyFilter(object obj, string property)
    {
      if (Filters.Count == 0)
        return true;

      string value = obj.GetType().GetProperty(property).GetValue(obj, null).ToString();
      return Filters.Contains(value);
    }

    public override void SerializeFilter()
    {
      foreach (var filter in Filters)
      {

      }
    }
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

    public static string[] s_KEYWORDS = {
      s_AND.ToString(),
      s_OR.ToString(),
      s_NULL
    };

    public static string[] s_ORAND = { s_AND.ToString(), s_OR.ToString() };

    public string Filter;
    private List<Tuple<BlockOperation, string>> m_filterblocks;

    private bool m_hasMacro;

    public TextFilter(string filter)
    {
      Filter = filter;//.ToLower();
      m_hasMacro = HasMacro();
      BuildFilterBlock();
    }

    public override bool ApplyFilter(object obj, string property)
    {
      if (Filter == "")
        return true;

      string value = obj.GetType().GetProperty(property).GetValue(obj, null).ToString().ToLower();
      if (m_hasMacro) //contains macro
      {
        return ApplyMacro(value);
      }
      else // no macro
      {
        return value.Contains(Filter.ToLower());
      }
    }

    public override void SerializeFilter()
    {

    }

    private bool HasMacro()
    {
      if (Filter == "")
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
          result &= ApplyMacroBlock(block.Item2, value);
        else
          result |= ApplyMacroBlock(block.Item2, value);
      }

      return result;
    }

    private bool ApplyMacroBlock(string filterBlock, string value)
    {
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
}
