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
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Microsoft.Win32;                      // For OpenFileDialog
using System.IO;                            // For StreamReader

namespace itestall
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // Bindingを使う際のバインド変数の宣言
        public string FileName { get; set; }
        public string Result { get; set; }
        // 解析結果表示モード　（０：ノード、１：トークン）
        public int anlMode = 0;
        // 解析結果ファイル
        public String TargetFile;

        public MainWindow()
        {
            InitializeComponent();

            // Bindingを使う際のデータコンテキストの設定
            this.DataContext = this;
        }

        /// <summary>
        /// ブロック処理のサンプルで置き換えるソースブロックを生成する処理
        /// </summary>
        /// <param name=""></param>
        /// <param name="e"></param>
        /// </summary>
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

        /// <summary>
        /// 指定されたファイルの解析
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// </summary>
        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            // 解析する C# のソースコード
            var sourceCode = @"
                using System;
 
                class Program
                {
                    static void Main()
                    {}
                }
            ";
            StreamReader sr = new StreamReader(TargetFile, Encoding.GetEncoding("UTF-8"));
            var sourceCode2 = sr.ReadToEnd();

            /// 画面をクリアする
            TxtbResult.Text = "";

            /// ファイルを解析する
            var syntaxTree = SyntaxTree.ParseText(sourceCode2);         // ソースコードをパースしてシンタックス ツリーに
            var rootNode = syntaxTree.GetRoot();                        // ルートのノードを取得

            // ブロック処理のサンプル
            // Main メソッドのブロックを取得
            //var block = rootNode.DescendantNodes().First(node => node.Kind == SyntaxKind.Block);

            //var newNode = rootNode.ReplaceNode(                       // ノードの置き換え
            //                    oldNode: block,                       // 元の空のブロック
            //                    newNode: CreateHelloWorldBlock()      // Console.WriteLine("Hello world!"); が入ったブロック
            //                );

            // 解析結果の表示
            if (anlMode == 0) { 
                Walker1 walker1 = new Walker1();                                        // 生成
                walker1.Node += new Walker1.NodeEventHandler(this.EventClass_Node);     // イベントハンドラーの登録

                walker1.Visit(rootNode);                                                // 解析
            } else {
                Walker2 walker2 = new Walker2();                                        // 生成
                walker2.Token += new Walker2.TokenEventHandler(this.EventClass_Token);  // イベントハンドラーの登録
                walker2.Visit(rootNode);                                                // 解析
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
            var radioButton = (RadioButton)sender;
            if (radioButton.Name.Equals("radioButton1")) {
                this.anlMode = 0;                                   // ノードモード
            } else
            {
                this.anlMode = 1;                                   // トークンモード
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
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
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
    }
}
