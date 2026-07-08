using DPSpecial.Utils;
using System.Windows;

namespace DPSpecial.MVVM.View
{
    /// <summary>
    /// Interaction logic for RebarProgressView.xaml
    /// </summary>
    public partial class ProgressView : Window
    {
        public ProgressView()
        {
            InitializeComponent();
            this.Escape();
        }
    }
}
