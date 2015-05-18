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
    //Nodeイベントで返されるデータ
    //ここではstring型のひとつのデータのみ返すものとする
    public class NodeEventArgs : EventArgs
    {
        public string Message;
    }


    public class Walker1 : SyntaxWalker // Visitor パターンでソースコードを解析 
    {
        //デリゲートの宣言
        //NodeEventArgs型のオブジェクトを返すようにする
        public delegate void NodeEventHandler(object sender, NodeEventArgs e);

        //イベントデリゲートの宣言
        public event NodeEventHandler Node;

        //イベント引数の宣言
        protected NodeEventArgs eventArg;

        protected virtual void OnNode(NodeEventArgs e)
        {
            if (Node != null)
            {
                Node(this, e);
            }
        }

        public Walker1()
        {
            eventArg = new NodeEventArgs();
        }

        public override void Visit(SyntaxNode node) // 各ノードを Visit
        {
            if (node != null) {
                // Console.WriteLine("[Node  - Type: {0}, Kind: {1}]\n{2}\n", node.GetType().Name, node.Kind, node);
                eventArg.Message = string.Format("[Node  - Type: {0}, Kind: {1}]\n{2}\n", node.GetType().Name, node.Kind(), node);
                OnNode(eventArg);
            }
            base.Visit(node);
        }

    }
}