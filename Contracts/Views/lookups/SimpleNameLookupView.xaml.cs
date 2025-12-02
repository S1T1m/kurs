using System.Windows.Controls;
using Contracts.ViewModels.lookups;

namespace Contracts.Views.lookups;

public partial class SimpleNameLookupView : UserControl
{
    public SimpleNameLookupView()
    {
        InitializeComponent();
        Loaded += (_,__) =>
        {
            if (Tag is string s && s.Contains('/'))
            {
                var parts = s.Split('/');
                DataContext = new SimpleNameLookupViewModel(App.DbFactory, parts[0], parts[1]);
            }
        };
    }
}