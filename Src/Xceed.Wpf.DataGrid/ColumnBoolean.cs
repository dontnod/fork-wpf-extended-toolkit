using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Xceed.Wpf.DataGrid
{
    public class ColumnBoolean : Column
    {
        public override FilterType GetFilterType() => FilterType.Boolean;
    }
}
