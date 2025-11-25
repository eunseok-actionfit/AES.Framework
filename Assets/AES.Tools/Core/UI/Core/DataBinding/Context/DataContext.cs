using System;
using System.ComponentModel;
using AES.Tools;


public sealed class DataContext
{
    public object ViewModel { get; private set; }
    public ViewModelContext ViewModelContext { get; private set; }
    public IBindingContext BindingContext => ViewModelContext;

    public void SetViewModel(object vm)
    {
        ViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        ViewModelContext = CreateContext(vm);
    }

    private ViewModelContext CreateContext(object vm)
    {
        if (vm is INotifyPropertyChanged inpc)
            return new NotifyPropertyChangedViewModelContext(inpc);

        return new ObservableViewModelContext(vm);
    }
}