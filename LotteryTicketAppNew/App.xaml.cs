using LotteryTicketAppNew.Views;

namespace LotteryTicketAppNew;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
} 