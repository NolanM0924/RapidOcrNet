using LotteryTicketAppNew.ViewModels;

namespace LotteryTicketAppNew.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
} 