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
//using Roslyn.Compilers;
//using Roslyn.Compilers.CSharp;
//using Roslyn.Compilers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace itestall
{
    //Tokenイベントで返されるデータ
    //string型のメッセージとノードの参照を返す
    public class TokenEventArgs : EventArgs
    {
        /// <summary>
        /// イベントのメッセージフィールド
        /// </summary>
        protected string message;
        public String Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        /// <summary>
        /// イベントのノードフィールド
        /// </summary>
        protected ItestallTreeNode node;
        public ItestallTreeNode Node
        {
            get
            {
                return node;
            }
            set
            {
                node = value;
            }
        }

    }


    public class Walker2 : SyntaxWalker // Visitor パターンでソースコードを解析
    {
        //デリゲートの宣言
        //NodeEventArgs型のオブジェクトを返すようにする
        public delegate void TokenEventHandler(object sender, TokenEventArgs e);

        //イベントデリゲートの宣言
        public event TokenEventHandler Token;

        //イベント引数の宣言
        TokenEventArgs eventArg;

        public Walker2() : base(depth: SyntaxWalkerDepth.Token) // トークンの深さまで Visit
        {
            eventArg = new TokenEventArgs();
        }

        protected virtual void OnToken(TokenEventArgs e)
        {
            if (Token != null)
            {
                Token(this, e);
            }
        }

        protected override void VisitToken(SyntaxToken token) // 各トークンを Visit
        {
            if (token != null)
            {
                // Console.WriteLine("[Node  - Type: {0}, Kind: {1}]\n{2}\n", node.GetType().Name, node.Kind, node);
                eventArg.Message = string.Format("[Token - Type: {0}, Kind: {1}]\n{2}\n", token.GetType().Name, token.Kind(), token);
                OnToken(eventArg);
            }
            base.VisitToken(token);
        }
    }
}