using System;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls;

public partial class BusyIndicator : UserControl
{
    public BusyIndicator()
    {
        InitializeComponent();
    }



    public Uri ItemSource
    {
        get { return (Uri)GetValue(ItemSourceProperty); }
        set { SetValue(ItemSourceProperty, value); }
    }

    public static readonly DependencyProperty ItemSourceProperty =
        DependencyProperty.Register("ItemSource", typeof(Uri), typeof(BusyIndicator));



    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(BusyIndicator));




}
