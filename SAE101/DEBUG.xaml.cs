using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SAE101
{
    /// <summary>
    /// Logique d'interaction pour DEBUG.xaml
    /// </summary>
    public partial class DEBUG : Window
    {
        public DEBUG()
        {
            InitializeComponent();
        }

        private void Pin_Click(object sender, RoutedEventArgs e)
        {
            // Inverser TopMost
            this.Topmost = !this.Topmost;
        }
    }
}
