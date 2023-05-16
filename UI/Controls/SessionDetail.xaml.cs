using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UI.Controls;

public partial class SessionDetail : UserControl
{
    public SessionDetail()
    {
        InitializeComponent();

        bool stats;
        if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            stats = true;
        }
        else
        {
            stats = false;
        }

        LoggedinTime = DateTime.Now.ToString();
        LastServerContact = DateTime.Now.ToString();
        SelectedActivity = "No activity";

        var time = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, delegate
        {
            if(!IsWaitingIndicatorVisible)
            {
                ActivityTime = ActivityTime.Add(TimeSpan.FromSeconds(1));

                if (!SelectedActivity?.Equals("Lunch") ?? false)
                {
                    SetValue(LoggedinTimeDependencyProperty, SessionTime);
                    SessionTime = SessionTime.Add(TimeSpan.FromSeconds(1));
                }

                SetValue(ActivityTimeDependencyProperty, ActivityTime);
            }

        }, Dispatcher);

        DataContext = this;
    }

    public TimeSpan SessionTime
    {
        get { return (TimeSpan)GetValue(LoggedinTimeDependencyProperty); }
        set { SetValue(LoggedinTimeDependencyProperty, value); }
    }

    public string LoggedinTime { get; set; }
    public string LastServerContact { get; set; }
    public string SelectedActivity { get; set; }

    public static readonly DependencyProperty LoggedinTimeDependencyProperty =
        DependencyProperty.Register("SessionTime", typeof(TimeSpan), typeof(SessionDetail), new PropertyMetadata(new TimeSpan(0, 0, 0)));

    public TimeSpan ActivityTime
    {
        get { return (TimeSpan)GetValue(ActivityTimeDependencyProperty); }
        set { SetValue(ActivityTimeDependencyProperty, value); }
    }

    public static readonly DependencyProperty ActivityTimeDependencyProperty =
        DependencyProperty.Register("ActivityTime", typeof(TimeSpan), typeof(SessionDetail), new PropertyMetadata(new TimeSpan(0, 0, 0)));

    public bool IsWaitingIndicatorVisible
    {
        get { return (bool)GetValue(IsWaitingIndicatorVisibleDependencyProperty); }
        set { SetValue(IsWaitingIndicatorVisibleDependencyProperty, value); }
    }

    public static readonly DependencyProperty IsWaitingIndicatorVisibleDependencyProperty =
        DependencyProperty.Register("IsWaitingIndicatorVisible", typeof(bool), typeof(SessionDetail), new PropertyMetadata(false));

    private void OnActivityChange_Click(object sender, RoutedEventArgs e)
    {
        SetValue(ActivityTimeDependencyProperty, new TimeSpan(0, 0, 0));
    }
}
