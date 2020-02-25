using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Xceed.Wpf.DataGrid
{
    public class ColumnComboBox : Column
    {
        #region ItemsList Property

        public static readonly DependencyProperty ItemsListProperty = DependencyProperty.Register(
          "ItemsList",
          typeof(IEnumerable),
          typeof(ColumnComboBox),
          new UIPropertyMetadata(null));

        public IEnumerable ItemsList
        {
            get
            {
                return (IEnumerable)this.GetValue(ColumnComboBox.ItemsListProperty);
            }
            set
            {
                this.SetValue(ColumnComboBox.ItemsListProperty, value);
            }
        }

        #endregion
    }
}
