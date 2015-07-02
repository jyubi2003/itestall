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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QuickGraph; // enables extension methods

namespace graphtest
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var edges = new SEdge<int>[] { new SEdge<int>(1, 2), new SEdge<int>(0, 1) };
            var graph = edges.ToAdjacencyGraph<int, SEdge<int>>(true /*edges*/);

            foreach (var vertex in graph.Vertices)
                foreach (var edge in graph.OutEdges(vertex))
                    Console.WriteLine(edge);
        }
    }
}
