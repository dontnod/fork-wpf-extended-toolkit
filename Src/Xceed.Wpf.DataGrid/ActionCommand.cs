using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Xceed.Wpf.DataGrid
{
  public class ActionCommand: ICommand
  {
    private readonly Func<Object, Boolean> _CanExecuteDelegate;
    private readonly Action<Object> _ExecuteDelegate;

    public ActionCommand(Action<Object> ExecuteDelegate, Func<Object, Boolean> CanExecuteDelegate = null)
    {
      Debug.Assert(ExecuteDelegate != null);
      _CanExecuteDelegate = CanExecuteDelegate;
      _ExecuteDelegate = ExecuteDelegate;
    }

    public event EventHandler CanExecuteChanged;

    public Boolean CanExecute(Object parameter)
    {
      return _CanExecuteDelegate == null || _CanExecuteDelegate(parameter);
    }

    public void Execute(Object parameter)
    {
      _ExecuteDelegate(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, new EventArgs());
    }
  }
}
