using DPSpecial.Utils;
using System.Windows;

namespace DPSpecial.MVVM.View
{
    public partial class ProgressView : Window
    {
        public ProgressView()
        {
            InitializeComponent();
            this.Escape();
        }
    }
}
