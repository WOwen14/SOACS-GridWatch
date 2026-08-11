using System;
using System.Windows.Input;
namespace SOACS.GridWatch.Services { public class RelayCommand : ICommand { readonly Action<object> _execute; readonly Predicate<object> _can; public RelayCommand(Action<object> e, Predicate<object> c=null){_execute=e;_can=c;} public bool CanExecute(object p){return _can==null||_can(p);} public void Execute(object p){_execute(p);} public event EventHandler CanExecuteChanged{add{CommandManager.RequerySuggested+=value;}remove{CommandManager.RequerySuggested-=value;}} } }
