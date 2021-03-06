﻿using System;
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
using Microsoft.CodeAnalysis;                   // For Microsoft Roslyn
using Microsoft.CodeAnalysis.CSharp;            // For Microsoft Roslyn
using Microsoft.CodeAnalysis.CSharp.Syntax;     // For Microsoft Roslyn

namespace itestall
{
    //Nodeイベントで返されるデータ
    //string型のメッセージとノードの参照を返す
    public class NodeEventArgs : EventArgs
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
                OnNode(eventArg);                   // デリゲート経由で画面のバッククラスのイベントハンドラを呼ぶ
            }
            base.Visit(node);
        }

    }
}