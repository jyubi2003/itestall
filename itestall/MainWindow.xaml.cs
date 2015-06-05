using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
//using Roslyn.Compilers;
//using Roslyn.Compilers.CSharp;
//using Roslyn.Compilers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;                            // For StreamReader
using System.Xml;                           // For XmlWriter
using System.Windows.Media.Imaging;         // For BitmapFrame
// using Microsoft.VisualStudio.TextManager.Interop;    // For TextSpan

namespace itestall
{
    public enum SyntaxCategory
    {
        None,
        SyntaxNode,
        SyntaxToken,
        SyntaxTrivia
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private class SyntaxTag
        {
            internal TextSpan Span { get; set; }
            internal TextSpan FullSpan { get; set; }
            internal TreeViewItem ParentItem { get; set; }
            internal string Kind { get; set; }
            internal SyntaxNode SyntaxNode { get; set; }
            internal SyntaxToken SyntaxToken { get; set; }
            internal SyntaxTrivia SyntaxTrivia { get; set; }
            internal SyntaxCategory Category { get; set; }
        }

        #region Private State
        private TreeViewItem _currentSelection;
        private bool _isNavigatingFromSourceToTree;
        private bool _isNavigatingFromTreeToSource;
        private readonly System.Windows.Forms.PropertyGrid _propertyGrid;
        private static readonly Thickness s_defaultBorderThickness = new Thickness(1);
        #endregion

        #region Public Properties, Events
        public SyntaxTree SyntaxTree { get; private set; }
        public SemanticModel SemanticModel { get; private set; }
        public bool IsLazy { get; private set; }

        public delegate void SyntaxNodeDelegate(SyntaxNode node);
        public event SyntaxNodeDelegate SyntaxNodeDirectedGraphRequested;
        public event SyntaxNodeDelegate SyntaxNodeNavigationToSourceRequested;

        public delegate void SyntaxTokenDelegate(SyntaxToken token);
        public event SyntaxTokenDelegate SyntaxTokenDirectedGraphRequested;
        public event SyntaxTokenDelegate SyntaxTokenNavigationToSourceRequested;

        public delegate void SyntaxTriviaDelegate(SyntaxTrivia trivia);
        public event SyntaxTriviaDelegate SyntaxTriviaDirectedGraphRequested;
        public event SyntaxTriviaDelegate SyntaxTriviaNavigationToSourceRequested;
        
        // Bindingを使う際のバインド変数の宣言
        public string FileName { get; set; }
        public string Result { get; set; }
        // 解析結果表示モード　（０：ノード、１：トークン、２：XML）
        public int anlMode = 0;
        // 解析結果ファイル
        public String TargetFile;
        #endregion

        #region Public Methods
        public MainWindow()
        {
            InitializeComponent();

            // Bindingを使う際のデータコンテキストの設定
            this.DataContext = this;

            // ウィンドウアイコンの設定
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/icon1.ico", UriKind.RelativeOrAbsolute));

            // プロパティウィンドウ部分の画面の初期化
            _propertyGrid = new System.Windows.Forms.PropertyGrid();
            _propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            _propertyGrid.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            _propertyGrid.HelpVisible = false;
            _propertyGrid.ToolbarVisible = false;
            _propertyGrid.CommandsVisibleIfAvailable = false;
            windowsFormsHost.Child = _propertyGrid;
        }
        public void Clear()
        {
            treeView.Items.Clear();
            _propertyGrid.SelectedObject = null;
            typeTextLabel.Visibility = Visibility.Hidden;
            kindTextLabel.Visibility = Visibility.Hidden;
            typeValueLabel.Content = string.Empty;
            kindValueLabel.Content = string.Empty;
            legendButton.Visibility = Visibility.Hidden;
        }

        // If lazy is true then treeview items are populated on-demand. In other words, when lazy is true
        // the children for any given item are only populated when the item is selected. If lazy is
        // false then the entire tree is populated at once (and this can result in bad performance when
        // displaying large trees).
        public void DisplaySyntaxTree(SyntaxTree tree, SemanticModel model = null, bool lazy = true)
        {
            if (tree != null)
            {
                IsLazy = lazy;
                SyntaxTree = tree;
                SemanticModel = model;
                AddNode(null, SyntaxTree.GetRoot());
                legendButton.Visibility = Visibility.Visible;
            }
        }

        // If lazy is true then treeview items are populated on-demand. In other words, when lazy is true
        // the children for any given item are only populated when the item is selected. If lazy is
        // false then the entire tree is populated at once (and this can result in bad performance when
        // displaying large trees).
        public void DisplaySyntaxNode(SyntaxNode node, SemanticModel model = null, bool lazy = true)
        {
            if (node != null)
            {
                IsLazy = lazy;
                SyntaxTree = node.SyntaxTree;
                SemanticModel = model;
                AddNode(null, node);
                legendButton.Visibility = Visibility.Visible;
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        public bool NavigateToBestMatch(int position, string kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false, string highlightLegendDescription = null)
        {
            TreeViewItem match = null;

            if (treeView.HasItems && !_isNavigatingFromTreeToSource)
            {
                _isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], position, kind, category);
                _isNavigatingFromSourceToTree = false;
            }

            var matchFound = match != null;

            if (highlightMatch && matchFound)
            {
                match.Background = Brushes.Yellow;
                match.BorderBrush = Brushes.Black;
                match.BorderThickness = s_defaultBorderThickness;
                highlightLegendTextLabel.Visibility = Visibility.Visible;
                highlightLegendDescriptionLabel.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(highlightLegendDescription))
                {
                    highlightLegendDescriptionLabel.Content = highlightLegendDescription;
                }
            }

            return matchFound;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(int start, int length, string kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false, string highlightLegendDescription = null)
        {
            return NavigateToBestMatch(new TextSpan(start, length), kind, category, highlightMatch, highlightLegendDescription);
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(TextSpan span, string kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false, string highlightLegendDescription = null)
        {
            TreeViewItem match = null;

            if (treeView.HasItems && !_isNavigatingFromTreeToSource)
            {
                _isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], span, kind, category);
                _isNavigatingFromSourceToTree = false;
            }

            var matchFound = match != null;

            if (highlightMatch && matchFound)
            {
                match.Background = Brushes.Yellow;
                match.BorderBrush = Brushes.Black;
                match.BorderThickness = s_defaultBorderThickness;
                highlightLegendTextLabel.Visibility = Visibility.Visible;
                highlightLegendDescriptionLabel.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(highlightLegendDescription))
                {
                    highlightLegendDescriptionLabel.Content = highlightLegendDescription;
                }
            }

            return matchFound;
        }
        #endregion

        /// <summary>
        /// ブロック処理のサンプルで置き換えるソースブロックを生成する処理
        /// </summary>
        /// <param name=""></param>
        /// <param name="e"></param>
        /// </summary>
        /*
        static BlockSyntax CreateHelloWorldBlock()
        {
            var invocationExpression = Syntax.InvocationExpression(       // Console.WriteLine("Hello world!");
                expression: Syntax.MemberAccessExpression(                // Console.WriteLine というメンバー アクセス
                    kind: SyntaxKind.MemberAccessExpression,
                    expression: Syntax.IdentifierName("Console"),
                    name: Syntax.IdentifierName("WriteLine")
                ),
                argumentList: Syntax.ArgumentList(                        // 引数リスト
                    arguments: Syntax.SeparatedList<ArgumentSyntax>(
                        node: Syntax.Argument(                            // "Hello world!"
                            expression: Syntax.LiteralExpression(
                                kind: SyntaxKind.StringLiteralExpression,
                                token: Syntax.Literal("Hello world!")
                            )
                        )
                    )
                )
            );

            var statement = Syntax.ExpressionStatement(expression: invocationExpression);
            return Syntax.Block(statement);
        }
        */

        #region Private Helpers - TreeView Navigation
        // Collapse all items in the treeview except for the supplied item. The supplied item
        // is also expanded, selected and scrolled into view.
        private void CollapseEverythingBut(TreeViewItem item)
        {
            if (item != null)
            {
                DeepCollapse((TreeViewItem)treeView.Items[0]);
                ExpandPathTo(item);
                item.IsSelected = true;
                item.BringIntoView();
            }
        }

        // Collapse the supplied treeview item including all its descendants.
        private void DeepCollapse(TreeViewItem item)
        {
            if (item != null)
            {
                item.IsExpanded = false;
                foreach (TreeViewItem child in item.Items)
                {
                    DeepCollapse(child);
                }
            }
        }

        // Ensure that the supplied treeview item and all its ancsestors are expanded.
        private void ExpandPathTo(TreeViewItem item)
        {
            if (item != null)
            {
                item.IsExpanded = true;
                ExpandPathTo(((SyntaxTag)item.Tag).ParentItem);
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        private TreeViewItem NavigateToBestMatch(TreeViewItem current, int position, string kind = null,
            SyntaxCategory category = SyntaxCategory.None)
        {
            TreeViewItem match = null;

            if (current != null)
            {
                SyntaxTag currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(position))
                {
                    CollapseEverythingBut(current);

                    foreach (TreeViewItem item in current.Items)
                    {
                        match = NavigateToBestMatch(item, position, kind, category);
                        if (match != null)
                        {
                            break;
                        }
                    }

                    if (match == null && (kind == null || currentTag.Kind == kind) &&
                       (category == SyntaxCategory.None || category == currentTag.Category))
                    {
                        match = current;
                    }
                }
            }

            return match;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        private TreeViewItem NavigateToBestMatch(TreeViewItem current, TextSpan span, string kind = null,
            SyntaxCategory category = SyntaxCategory.None)
        {
            TreeViewItem match = null;

            if (current != null)
            {
                SyntaxTag currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(span))
                {
                    if ((currentTag.Span == span || currentTag.FullSpan == span) && (kind == null || currentTag.Kind == kind))
                    {
                        CollapseEverythingBut(current);
                        match = current;
                    }
                    else
                    {
                        CollapseEverythingBut(current);

                        foreach (TreeViewItem item in current.Items)
                        {
                            match = NavigateToBestMatch(item, span, kind, category);
                            if (match != null)
                            {
                                break;
                            }
                        }

                        if (match == null && (kind == null || currentTag.Kind == kind) &&
                           (category == SyntaxCategory.None || category == currentTag.Category))
                        {
                            match = current;
                        }
                    }
                }
            }

            return match;
        }
        #endregion

        #region Private Helpers - TreeView Population
        // Helpers for populating the treeview.

        private void AddNodeOrToken(TreeViewItem parentItem, SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsNode)
            {
                AddNode(parentItem, nodeOrToken.AsNode());
            }
            else
            {
                AddToken(parentItem, nodeOrToken.AsToken());
            }
        }

        private void AddNode(TreeViewItem parentItem, SyntaxNode node)
        {
            var kind = node.Kind().ToString();
            var tag = new SyntaxTag()
            {
                SyntaxNode = node,
                Category = SyntaxCategory.SyntaxNode,
                Span = node.Span,
                FullSpan = node.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = false,
                Foreground = Brushes.Blue,
                Background = node.ContainsDiagnostics ? Brushes.Pink : Brushes.White,
                Header = tag.Kind + " " + node.Span.ToString()
            };

            if (SyntaxTree != null && node.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(node))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = node.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = node;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxNodeNavigationToSourceRequested != null)
                {
                    SyntaxNodeNavigationToSourceRequested(node);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        AddNodeOrToken(item, child);
                    }
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (node.ChildNodesAndTokens().Count > 0)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        AddNodeOrToken(item, child);
                    }
                }
            }
        }

        private void AddToken(TreeViewItem parentItem, SyntaxToken token)
        {
            var kind = token.Kind().ToString();
            var tag = new SyntaxTag()
            {
                SyntaxToken = token,
                Category = SyntaxCategory.SyntaxToken,
                Span = token.Span,
                FullSpan = token.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = false,
                Foreground = Brushes.DarkGreen,
                Background = token.ContainsDiagnostics ? Brushes.Pink : Brushes.White,
                Header = tag.Kind + " " + token.Span.ToString()
            };

            if (SyntaxTree != null && token.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(token))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = token.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = token;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxTokenNavigationToSourceRequested != null)
                {
                    SyntaxTokenNavigationToSourceRequested(token);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddTrivia(item, trivia, true);
                    }

                    foreach (var trivia in token.TrailingTrivia)
                    {
                        AddTrivia(item, trivia, false);
                    }
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (token.HasLeadingTrivia || token.HasTrailingTrivia)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddTrivia(item, trivia, true);
                    }

                    foreach (var trivia in token.TrailingTrivia)
                    {
                        AddTrivia(item, trivia, false);
                    }
                }
            }
        }

        private void AddTrivia(TreeViewItem parentItem, SyntaxTrivia trivia, bool isLeadingTrivia)
        {
            var kind = trivia.Kind().ToString();
            var tag = new SyntaxTag()
            {
                SyntaxTrivia = trivia,
                Category = SyntaxCategory.SyntaxTrivia,
                Span = trivia.Span,
                FullSpan = trivia.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = false,
                Foreground = Brushes.Maroon,
                Background = trivia.ContainsDiagnostics ? Brushes.Pink : Brushes.White,
                Header = (isLeadingTrivia ? "Lead: " : "Trail: ") + tag.Kind + " " + trivia.Span.ToString()
            };

            if (SyntaxTree != null && trivia.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(trivia))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = trivia.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = trivia;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxTriviaNavigationToSourceRequested != null)
                {
                    SyntaxTriviaNavigationToSourceRequested(trivia);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    AddNode(item, trivia.GetStructure());
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
                typeTextLabel.Visibility = Visibility.Hidden;
                kindTextLabel.Visibility = Visibility.Hidden;
                typeValueLabel.Content = string.Empty;
                kindValueLabel.Content = string.Empty;
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (trivia.HasStructure)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    AddNode(item, trivia.GetStructure());
                }
            }
        }
        #endregion

        #region Private Helpers - Other
        private void DisplaySymbolInPropertyGrid(ISymbol symbol)
        {
            if (symbol == null)
            {
                typeTextLabel.Visibility = Visibility.Hidden;
                kindTextLabel.Visibility = Visibility.Hidden;
                typeValueLabel.Content = string.Empty;
                kindValueLabel.Content = string.Empty;
            }
            else
            {
                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = symbol.GetType().Name;
                kindValueLabel.Content = symbol.Kind.ToString();
            }

            _propertyGrid.SelectedObject = symbol;
        }

        private static TreeViewItem FindTreeViewItem(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return (TreeViewItem)source;
        }
        #endregion

        #region Event Handlers
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem != null)
            {
                _currentSelection = (TreeViewItem)treeView.SelectedItem;
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindTreeViewItem((DependencyObject)e.OriginalSource);

            if (item != null)
            {
                item.Focus();
            }
        }

        private void TreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var directedSyntaxGraphEnabled =
                (SyntaxNodeDirectedGraphRequested != null) &&
                (SyntaxTokenDirectedGraphRequested != null) &&
                (SyntaxTriviaDirectedGraphRequested != null);

            var symbolDetailsEnabled =
                (SemanticModel != null) &&
                (((SyntaxTag)_currentSelection.Tag).Category == SyntaxCategory.SyntaxNode);

            if ((!directedSyntaxGraphEnabled) && (!symbolDetailsEnabled))
            {
                e.Handled = true;
            }
            else
            {
                directedSyntaxGraphMenuItem.Visibility = directedSyntaxGraphEnabled ? Visibility.Visible : Visibility.Collapsed;
                symbolDetailsMenuItem.Visibility = symbolDetailsEnabled ? Visibility.Visible : Visibility.Collapsed;
                typeSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                convertedTypeSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                aliasSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                constantValueDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                menuItemSeparator1.Visibility = symbolDetailsMenuItem.Visibility;
                menuItemSeparator2.Visibility = symbolDetailsMenuItem.Visibility;
            }
        }

        private void DirectedSyntaxGraphMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection != null)
            {
                var currentTag = (SyntaxTag)_currentSelection.Tag;

                if (currentTag.Category == SyntaxCategory.SyntaxNode && SyntaxNodeDirectedGraphRequested != null)
                {
                    SyntaxNodeDirectedGraphRequested(currentTag.SyntaxNode);
                }
                else if (currentTag.Category == SyntaxCategory.SyntaxToken && SyntaxTokenDirectedGraphRequested != null)
                {
                    SyntaxTokenDirectedGraphRequested(currentTag.SyntaxToken);
                }
                else if (currentTag.Category == SyntaxCategory.SyntaxTrivia && SyntaxTriviaDirectedGraphRequested != null)
                {
                    SyntaxTriviaDirectedGraphRequested(currentTag.SyntaxTrivia);
                }
            }
        }

        private void SymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetSymbolInfo(currentTag.SyntaxNode).Symbol;
                if (symbol == null)
                {
                    symbol = SemanticModel.GetDeclaredSymbol(currentTag.SyntaxNode);
                }

                if (symbol == null)
                {
                    symbol = SemanticModel.GetPreprocessingSymbolInfo(currentTag.SyntaxNode).Symbol;
                }

                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void TypeSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetTypeInfo(currentTag.SyntaxNode).Type;
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void ConvertedTypeSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetTypeInfo(currentTag.SyntaxNode).ConvertedType;
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void AliasSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetAliasInfo(currentTag.SyntaxNode);
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void ConstantValueDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var value = SemanticModel.GetConstantValue(currentTag.SyntaxNode);
                kindTextLabel.Visibility = Visibility.Hidden;
                kindValueLabel.Content = string.Empty;

                if (!value.HasValue)
                {
                    typeTextLabel.Visibility = Visibility.Hidden;
                    typeValueLabel.Content = string.Empty;
                    _propertyGrid.SelectedObject = null;
                }
                else
                {
                    typeTextLabel.Visibility = Visibility.Visible;
                    typeValueLabel.Content = value.Value.GetType().Name;
                    _propertyGrid.SelectedObject = value;
                }
            }
        }

        private void LegendButton_Click(object sender, RoutedEventArgs e)
        {
            legendPopup.IsOpen = true;
        }

        /// <summary>
        /// 指定されたファイルの解析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            /// 画面をクリアする
            TxtbResult.Text = "";

            // 指定されたファイルを読み込む
            StreamReader sr = new StreamReader(TargetFile, Encoding.GetEncoding("UTF-8"));
            var sourceCode = sr.ReadToEnd();

            /// ファイルを解析する
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);        // ソースコードをパースしてシンタックス ツリーに
            var rootNode = syntaxTree.GetRoot();                            // ルートのノードを取得

            // ツリービューを表示する
            // AddNode(null, rootNode);
            DisplaySyntaxNode(rootNode);

            // ツリーノードのルートを作る
            //ItestallTreeNode root = new ItestallTreeNode();

            //ItestallTreeNode temp = new ItestallTreeNode();
            //temp.Name = "Child1";
            //root.AddChild(temp);

            //temp = new ItestallTreeNode();
            //temp.Name = "Child2";
            //root.AddChild(temp);

            // ブロック処理のサンプル
            // Main メソッドのブロックを取得
            //var block = rootNode.DescendantNodes().First(node => node.Kind == SyntaxKind.Block);

            //var newNode = rootNode.ReplaceNode(                       // ノードの置き換え
            //                    oldNode: block,                       // 元の空のブロック
            //                    newNode: CreateHelloWorldBlock()      // Console.WriteLine("Hello world!"); が入ったブロック
            //                );

            // 解析結果の表示
            if (anlMode == 0)
            {
                Walker1 walker1 = new Walker1();                                        // 生成
                walker1.Node += new Walker1.NodeEventHandler(this.EventClass_Node);     // イベントハンドラーの登録
                walker1.Visit(rootNode);                                                // 解析

            }
            else if (anlMode == 1)
            {
                Walker2 walker2 = new Walker2();                                        // 生成
                walker2.Token += new Walker2.TokenEventHandler(this.EventClass_Token);  // イベントハンドラーの登録
                walker2.Visit(rootNode);                                                // 解析

            }
            else
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                using (Stream stream = new MemoryStream())
                {
                    using (XmlWriter writer = XmlWriter.Create(stream, settings))
                    {
                        new Walker3(writer).Visit(rootNode);
                    }

                    //
                    // ストリームのポジションを戻してから出力.
                    //
                    stream.Position = 0;
                    TxtbResult.Text += new StreamReader(stream).ReadToEnd();
                }
            }


        }

        /// <summary>
        /// ノードモードのときのイベントハンドラの処理本体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void EventClass_Node(object sender, NodeEventArgs e)
        {
            //返されたデータを取得し表示
            TxtbResult.Text += e.Message;
        }

        /// <summary>
        /// トークンモードのときのイベントハンドラの処理本体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void EventClass_Token(object sender, TokenEventArgs e)
        {
            //返されたデータを取得し表示
            TxtbResult.Text += e.Message;
        }

        /// <summary>
        /// モードを切り替えるラジオボタンがチェックされた時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = (System.Windows.Controls.RadioButton)sender;
            if (radioButton.Name.Equals("radioButton1")) {
                this.anlMode = 0;                                   // ノードモード
            }
            else if (radioButton.Name.Equals("radioButton2"))
            {
                this.anlMode = 1;                                   // トークンモード
            }
            else
            {
                this.anlMode = 2;                                   // XML出力モード
            }
        }

        /// <summary>
        /// 対象ファイル選択ボタンのクリック処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void BtnRef_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.FileName = "";
            openFileDialog1.DefaultExt = "*.cs";
            if (openFileDialog1.ShowDialog() == true)
            {
                TargetFile = openFileDialog1.FileName;
                TxtbSrc.Text = openFileDialog1.FileName;
            }

        }

        /// <summary>
        /// 終了ボタンクリック時処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void BtnEnd_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// キャンセルボタンのクリック時処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 結果表示テキストボックスの初期化処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void TxtbResult_Initialized(object sender, EventArgs e)
        {

        }

        #endregion
    }
}
