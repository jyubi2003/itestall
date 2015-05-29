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
using System.Windows.Forms;
//using Roslyn.Compilers;
//using Roslyn.Compilers.CSharp;
//using Roslyn.Compilers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Win32;                      // For OpenFileDialog
using System.IO;                            // For StreamReader
using System.Xml;                           // For XmlWriter
using Microsoft.VisualStudio.TextManager.Interop;    // For TextSpan

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

            // ツリーノードのルートを作る
            ItestallTreeNode root = new ItestallTreeNode();

            ItestallTreeNode temp = new ItestallTreeNode();
            temp.Name = "Child1";
            root.AddChild(temp);

            temp = new ItestallTreeNode();
            temp.Name = "Child2";
            root.AddChild(temp);

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

        /// <summary>
        /// 結果表示テキストボックスの初期化処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void LegendButton_Click(object sender, EventArgs e)
        {
            legendPopup.IsOpen = true;
        }
        #endregion
    }
}
