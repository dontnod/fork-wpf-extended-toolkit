using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xceed.Wpf.DataGrid
{
    public abstract class IFilter
    {
        public abstract bool ApplyFilter(object obj, string property);
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
    }

    public class TextFilter: IFilter
    {
        public string Filter;

        public TextFilter(string filter)
        {
            Filter = filter.ToLower();
        }

        public override bool ApplyFilter(object obj, string property)
        {
            if (Filter == "")
                return true;

            string value = obj.GetType().GetProperty(property).GetValue(obj, null).ToString().ToLower();
            return value.Contains(Filter);
        }
    }
}
