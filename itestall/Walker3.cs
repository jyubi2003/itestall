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
using System.IO;
using System.Xml;


namespace itestall
{
    class Walker3 : SyntaxWalker
    {
        /*
        //デリゲートの宣言
        //NodeEventArgs型のオブジェクトを返すようにする
        public delegate void NodeEventHandler(object sender, NodeEventArgs e);

        //イベントデリゲートの宣言
        public event NodeEventHandler Node;

        private NodeEventArgs eventArg;

        protected virtual void OnNode(NodeEventArgs e)
        {
            if (Node != null)
            {
                Node(this, e);
            }
        }
        */
        private XmlWriter writer;

        public Walker3(XmlWriter argWriter)
        {
            writer = argWriter;
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            writer.WriteStartDocument();
            DefaultVisit(node);
            writer.WriteEndDocument();
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            openNode(node);
            writer.WriteAttributeString("PlainName", node.ToString());
            closeNode(node);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            openNode(node);
            base.DefaultVisit(node);
            closeNode(node);
        }

        private void openNode(SyntaxNode node)
        {
            Type type = node.GetType();
            writer.WriteStartElement(type.Name);
        }

        private void closeNode(SyntaxNode node)
        {
            writer.WriteEndElement();
        }
    }
}
