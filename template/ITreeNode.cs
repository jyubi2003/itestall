using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// itestallアプリケーション個別のテンプレート名前空間
/// </summary>
/// <typeparam name="T"></typeparam>
namespace itestall.treenode
{
    /// <summary>
    /// ツリー構造のインターフェース
    /// </summary>
    public interface ITreeNode<T>
    {
        T Parent { get; set; }
        IList<T> Children { get; set; }

        T AddChild(T child);
        T RemoveChild(T child);
        bool TryRemoveChild(T child);
        T ClearChildren();
        bool TryRemoveOwn();
        T RemoveOwn();
    }
}