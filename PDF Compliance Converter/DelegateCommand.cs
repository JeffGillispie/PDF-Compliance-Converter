using System;
using System.Windows.Input;

namespace PDF_Compliance_Converter
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object> parameteredAction;
        private readonly Action parameterlessAction;

        public DelegateCommand(Action<object> action)
        {
            this.parameteredAction = action;
        }

        public DelegateCommand(Action action)
        {
            this.parameterlessAction = action;
        }

        public void Execute(object parameter)
        {
            if (parameteredAction != null)
            {
                parameteredAction(parameter);
            }
            else
            {
                parameterlessAction();
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
