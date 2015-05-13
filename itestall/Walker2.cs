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

namespace itestall
{
    //Nodeイベントで返されるデータ
    //ここではstring型のひとつのデータのみ返すものとする
    public class TokenEventArgs : EventArgs
    {
        public string Message;
    }


    public class Walker2 : SyntaxWalker // Visitor パターンでソースコードを解析
    {
        //デリゲートの宣言
        //NodeEventArgs型のオブジェクトを返すようにする
        public delegate void TokenEventHandler(object sender, TokenEventArgs e);

        //イベントデリゲートの宣言
        public event TokenEventHandler Token;

        public Walker2() : base(depth: SyntaxWalkerDepth.Token) // トークンの深さまで Visit
        { }

        protected virtual void OnToken(TokenEventArgs e)
        {
            if (Token != null)
            {
                Token(this, e);
            }
        }

        public override void VisitToken(SyntaxToken token) // 各トークンを Visit
        {
            if (token != null)
            {
                // Console.WriteLine("[Node  - Type: {0}, Kind: {1}]\n{2}\n", node.GetType().Name, node.Kind, node);
                TokenEventArgs eventArg = new TokenEventArgs();
                eventArg.Message = string.Format("[Token - Type: {0}, Kind: {1}]\n{2}\n", token.GetType().Name, token.Kind, token);
                OnToken(eventArg);
            }
            base.VisitToken(token);
        }
    }
}