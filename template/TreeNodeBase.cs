/// <summary>
/// 簡易ツリー構造のジェネリッククラス
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TreeNodeBase<T> : ITreeNode<TreeNodeBase<T>> where T : class
{
    /// <summary>
    /// 親への参照フィールド
    /// </summary>
    protected TreeNodeBase<T> parent = null;

    /// <summary>
    /// 親への参照プロパティ
    /// </summary>
    public virtual TreeNodeBase<T> Parent
    {
        get
        {
            return parent;
        }
        set
        {
            parent = value;
        }
    }

    /// <summary>
    /// 子ノードのリストフィールド
    /// </summary>
    protected IList<TreeNodeBase<T>> children = null;

    /// <summary>
    /// 子ノードのリストプロパティ
    /// </summary>
    public virtual IList<TreeNodeBase<T>> Children
    {
        get
        {
            if (children == null)
                children = new List<TreeNodeBase<T>>();
            return children;
        }
        set
        {
            children = value;
        }
    }

    /// <summary>
    /// 子ノードを追加する。
    /// </summary>
    /// <param name="child">追加したいノード</param>
    /// <returns>追加後のオブジェクト</returns>
    public virtual TreeNodeBase<T> AddChild(TreeNodeBase<T> child)
    {
        if (child == null)
            throw new ArgumentNullException("Adding tree child is null.");

        this.Children.Add(child);
        child.Parent = this;

        return this;
    }

    /// <summary>
    /// 子ノードを削除する。
    /// </summary>
    /// <param name="child">削除したいノード</param>
    /// <returns>削除後のオブジェクト</returns>
    public virtual TreeNodeBase<T> RemoveChild(TreeNodeBase<T> child)
    {
        this.Children.Remove(child);
        return this;
    }

    /// <summary>
    /// 子ノードを削除する。
    /// </summary>
    /// <param name="child">削除したいノード</param>
    /// <returns>削除の可否</returns>
    public virtual bool TryRemoveChild(TreeNodeBase<T> child)
    {
        return this.Children.Remove(child);
    }

    /// <summary>
    /// 子ノードを全て削除する。
    /// </summary>
    /// <returns>子ノードを全削除後のオブジェクト</returns>
    public virtual TreeNodeBase<T> ClearChildren()
    {
        this.Children.Clear();
        return this;
    }

    /// <summary>
    /// 自身のノードを親ツリーから削除する。
    /// </summary>
    /// <returns>親のオブジェクト</returns>
    public virtual TreeNodeBase<T> RemoveOwn()
    {
        TreeNodeBase<T> parent = this.Parent;
        parent.RemoveChild(this);
        return parent;
    }

    /// <summary>
    /// 自身のノードを親ツリーから削除する。
    /// </summary>
    /// <returns>削除の可否</returns>
    public virtual bool TryRemoveOwn()
    {
        TreeNodeBase<T> parent = this.Parent;
        return parent.TryRemoveChild(this);
    }

}
